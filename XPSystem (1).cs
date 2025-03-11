using System;
using System.Globalization;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;
using Oxide.Core.Libraries;
using System.Collections.Generic;
using Oxide.Game.Rust.Cui;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using UnityEngine;
using VLB;

namespace Oxide.Plugins
{
    [Info("XPSystem", "http://topplugin.ru/", "0.0.8")]
	[Description("Добавляет на сервер XP Систему, благодаря которой, игроки смогут получать баланс в магазине за фарм.")]
    class XPSystem : RustPlugin
    {

        [PluginReference] Plugin ImageLibrary;
		private string Layer = "UI_XPSYSTEM";

        public ulong lastDamageName;
		
		private readonly Dictionary<string, int> playerInfo = new Dictionary<string, int>();
		private readonly List<uint> crateInfo = new List<uint>();
		private static Configuration Settings = new Configuration();
		
        #region Configuration [Конфиг] 
       
	    private class Configuration
        {
            public class API
            {
				[JsonProperty("APIKey (Секретный ключ)")] 
				public string SecretKey = "";
				[JsonProperty("ShopID (ИД магазина в сервисе)")]
				public string ShopID = "";
			}
			
			public class Balance
			{
				[JsonProperty("Сколько рублей на баланс магазина будет выдаваться")]
				public int Money = 5; 
			}
			
			public class Gather
			{
				[JsonProperty("Сколько игроку будет выдаваться XP за подбирание ресурсов")]
				public float CollectiblePickup = 0.15f; //Settings.GatherSettings.CollectiblePickup
				[JsonProperty("Сколько игроку будет выдаваться XP за подбирание плантации")]
				public float CropGather = 0.15f; //Settings.GatherSettings.CropGather
				[JsonProperty("Сколько игроку будет выдаваться XP за фарм бочек")]
				public float BarrelGather = 0.5f; //Settings.GatherSettings.BarrelGather
				[JsonProperty("Сколько игроку будет выдаваться XP за фарм ящиков")]
				public float CrateGather = 0.5f; //Settings.GatherSettings.CrateGather
                [JsonProperty("Сколько игроку будет выдаваться XP за убийство животного")]
				public float AnimalGather = 0.5f; //Settings.GatherSettings.AnimalGather
            }

            public class GathersOptions
			{
				[JsonProperty("Сколько игроку будет выдаваться XP за фарм дерева")]
				public float WoodGathers = 0.5f; //Settings.GathersSetting.WoodGathers
                [JsonProperty("Сколько игроку будет выдаваться XP за фарм обычного камня stones")]
				public float StonesGathers = 0.5f; //Settings.GathersSetting.StonesGathers
                [JsonProperty("Сколько игроку будет выдаваться XP за фарм металлического камня")]
				public float MetalOreGathers = 0.5f; //Settings.GathersSetting.MetalOreGathers
                [JsonProperty("Сколько игроку будет выдаваться XP за фарм серного камня")]
				public float SulfurGathers = 0.5f; //Settings.GathersSetting.SulfurGathers
                [JsonProperty("Сколько игроку будет выдаваться XP за фарм бонус МВК")]
				public float HQMGathers = 0.5f; //Settings.GathersSetting.HQMGathers						
			}
			
			public class GUI
			{
				[JsonProperty("Разрешение для использования команды /xp")]
				public string XpPermission = "xpsystem.use";
                [JsonProperty("Текст в основном блоке (возле аватарки)")]
                public string MainBlockText = "<color=#a5e664><b>XP</b></color> можно заработать путём:\n\n<color=#a5e664><b>—</b></color> Подбирания ресурсов\n<color=#a5e664><b>—</b></color> Фарма руды и деревьев\n<color=#a5e664><b>—</b></color> Выращивания еды (плантации)\n<color=#a5e664><b>—</b></color> Фарма бочек и ящиков\n<color=#a5e664><b>—</b></color> Охоты на людей и животных";	
                [JsonProperty("Текст во втором нижнем блоке")]
				public string SecondBlockText = "Перед выводом <color=#a5e664><b>XP</b></color> убедитесь, что Вы авторизованы в нашем магазине <color=#a5e664>SHOP.GAMESTORES.SU</color>";				
			}
			
			[JsonProperty("Настройки API плагина")]
            public API APISettings = new API();
            [JsonProperty("Настройка кол-ва выдачи баланса игроку")]
            public Balance BalanceSettings = new Balance();
			[JsonProperty("Настройка кол-ва выдачи XP с фарма ресурсов")]
            public Gather GatherSettings = new Gather();
            [JsonProperty("Более подробная настройка кол-ва выдачи XP с фарма ресурсов и животных")]
            public GathersOptions GathersSetting = new GathersOptions();
			[JsonProperty("Настройка интерфейса")]
            public GUI GUISettings = new GUI();
		}
		
        #endregion
		
		#region ConfigLoad / DataFiles [Загрузка конфига и информация в дате]
		
		protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                Settings = Config.ReadObject<Configuration>();
                if (Settings?.APISettings == null) LoadDefaultConfig();
            }
            catch
            {
                PrintWarning($"Ошибка в конфиге... Создаю новую конфигурацию!");
                LoadDefaultConfig();
            }
            
            NextTick(SaveConfig);
        }

        protected override void LoadDefaultConfig()
        { 
			PrintWarning(
			                           "Благодарю за приобретение плагина от разработчика плагина: " +
			                           "mabe. Если будут какие-то вопросы, писать - vk.com/zaebokuser");
            Settings = new Configuration();
        } 
        
        protected override void SaveConfig() => Config.WriteObject(Settings);
		
		private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            SaveData();
        }
		
		private void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, Layer);
            }
			SaveData();
        }
		
		public class Xp
		{
			[JsonProperty("Количество XP у игрока:")]
			public float XP { get; set; }
		}
			
		private Dictionary<ulong, Xp> XPS { get; set; }
		
		#endregion
        
        #region GameStores [Запрос в GameStores для обмена XP на рубли] 
		
		bool LogsPlayer = true;

        void MoneyPlus(ulong userId, int amount) {
			ApiRequestBalance(new Dictionary<string, string>() {
				{"action", "moneys"},
				{"type", "plus"},
				{"steam_id", userId.ToString()},
				{"amount", amount.ToString()},
                { "mess", "Обмен XP на рубли! Спасибо, что играете у нас!"}
			});
		}

		void ApiRequestBalance(Dictionary<string, string> args) {
			string url =
				$"https://gamestores.ru/api?shop_id={Settings.APISettings.ShopID}&secret={Settings.APISettings.SecretKey}{string.Join("", args.Select(arg => $"&{arg.Key}={arg.Value}").ToArray())}";
			webrequest.EnqueueGet(url,
				(i, s) => {
					if (i != 200 && i != 201) {
						PrintError($"{url}\nCODE {i}: {s}");

						if (LogsPlayer) {
							LogToFile("logError", $"({DateTime.Now.ToShortTimeString()}): {url}\nCODE {i}: {s}", this);
						}
					} else {
						if (LogsPlayer) {
							LogToFile("logWEB",
								$"({DateTime.Now.ToShortTimeString()}): "
							+ "Пополнение счета:"
							+ $"{string.Join(" ", args.Select(arg => $"{arg.Value}").ToArray()).Replace("moneys", "").Replace("plus", "")}",
								this);
						}
					}

					if (i == 201) {
						PrintWarning("Плагин не работает!");
						Interface.Oxide.UnloadPlugin(Title);
					}
				},
				this);
		}

        #endregion
		
		#region XPFromGather [Добыча]
        
        object OnCollectiblePickup(Item item, BasePlayer player)
        {
            XPS[player.userID].XP += Settings.GatherSettings.CollectiblePickup;
			
            return null;
        }
        
        object OnCropGather(GrowableEntity plant, Item item, BasePlayer player)
        {
            XPS[player.userID].XP += Settings.GatherSettings.CropGather;
			
            return null;
        }
        
        void OnDispenserBonus(ResourceDispenser dispenser, BasePlayer player, Item item)
        {
            ProcessItem(player, item);
            return;
        }

        void ProcessItem(BasePlayer player, Item item)
        {
            switch (item.info.shortname)
            {
                case "wood":
                    XPS[player.userID].XP += Settings.GathersSetting.WoodGathers;
                    return;
                    break;
                case "stones":
                    XPS[player.userID].XP += Settings.GathersSetting.StonesGathers;
                    return;
                    break;
                case "metal.ore":
                    XPS[player.userID].XP += Settings.GathersSetting.MetalOreGathers;
                    return;
                    break;
                case "sulfur.ore":
                    XPS[player.userID].XP += Settings.GathersSetting.SulfurGathers;
                    return;
                    break;
                case "hq.metal.ore":
                    XPS[player.userID].XP += Settings.GathersSetting.HQMGathers;
                    return;
                    break;
            }
        }

        private void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null) { return; }
            if (info == null) { return; }
            if (info.InitiatorPlayer == null) { return; }
            if (entity is BaseAnimalNPC)
            {
                BasePlayer player = info.InitiatorPlayer;
                XPS[player.userID].XP += Settings.GatherSettings.AnimalGather;
                return;
            }
            if (entity.PrefabName.Contains("barrel"))
            {
                BasePlayer player = info?.InitiatorPlayer;
                XPS[player.userID].XP += Settings.GatherSettings.BarrelGather;
                return;
            }
        }

		private void OnLootEntity(BasePlayer player, BaseEntity entity)
        {
            if (!entity.ShortPrefabName.Contains("crate_") && entity.ShortPrefabName != "heli_crate")
                return;

            if (crateInfo.Contains(entity.net.ID))
                return;

            crateInfo.Add(entity.net.ID);
			XPS[player.userID].XP += Settings.GatherSettings.CrateGather;
        }
		
        #endregion

        #region Hooks [Хуки/проверка]

        private void OnServerInitialized()
        {   
		
			permission.RegisterPermission(Settings.GUISettings.XpPermission, this);
			
			PrintWarning(
			                           "Благодарю за скачивание плагинас форума: " +
			                           "https://topplugin.ru/");
									   
			if (Interface.Oxide.DataFileSystem.ExistsDatafile("XPSystem/Database"))
            { 
                XPS = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, Xp>>("XPSystem/Database");
            }
            else
            {
                XPS = new Dictionary<ulong, Xp>();
            }

            foreach (var player in BasePlayer.activePlayerList)
                GrowableEntity(player);
            SaveData();
        }
        
        private void GrowableEntity(BasePlayer player)
        {
            if (!XPS.ContainsKey(player.userID))
                XPS.Add(player.userID, new Xp { XP = 0 });
			SaveData();
        }

		private void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject("XPSystem/Database", XPS);
        }
		
        #endregion

        #region Commands [Команды]

        [ChatCommand("xp")]
        void cmdXp(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, Settings.GUISettings.XpPermission))
            {
                SendReply(player, "У вас <color=#a5e664>недостаточно</color> прав для использования этой команды!");
                return;
            }

            DrawGUI(player);
        }
		
		[ConsoleCommand("xp")]
        private void cmdXp(ConsoleSystem.Arg args)
        {
            if (!args.Player()) return;
            DrawGUI(args.Player());
        }
		
		[ConsoleCommand("xpsell")]
        private void CmdXpSell(ConsoleSystem.Arg args)
        {
            BasePlayer player = args.Player();

                if (XPS[player.userID].XP >= 100)
                    {
                        SendReply(player,
							$"Перевод <color=#a5e664>XP в рубли</color> успешно произведён!\nПроверьте свой баланс в магазине.\n<color=#a5e664>В случае не поступления средств сообщите администрации!</color>");
                        XPS[player.userID].XP -= 100;
                        MoneyPlus(player.userID, Settings.BalanceSettings.Money);
                        return;
                    } 
					
				else
                    {
                        SendReply(player,
                            $"<color=#a5e664>Обмен не произведён!</color>\nДля обмена нужно иметь <color=#a5e664>100 XP</color>!\n У вас <color=#a5e664>{XPS[player.userID].XP} XP</color>!");
                        return;
                    }
        }
		
		[ConsoleCommand("xp.give")]
        private void CmdXpgv(ConsoleSystem.Arg args)
        {
            BasePlayer player = args.Player();

                BasePlayer target = BasePlayer.Find(args.Args[0]);
                if (target != null)
                {
                    if (XPS.ContainsKey(target.userID))
                    {
                        XPS[target.userID].XP += int.Parse(args.Args[1]);
                        Puts("Баланс успешно изменен");
                    }
                }
            
        }

        #endregion
		
		#region GUI [Интерфейс]
		
		private void DrawGUI(BasePlayer player)
		{	
			CuiHelper.DestroyUi(player, Layer);
			var container = new CuiElementContainer();			
            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Image = { Color = HexToRustFormat("#202020C1"), Material = "assets/content/ui/uibackgroundblur.mat" }
            }, "Overlay", Layer);
			container.Add(new CuiButton //Фононовое закрытие
            {
                RectTransform = { AnchorMin = "-1 -1", AnchorMax = "1.3 1.3", OffsetMax = "0 0" },
                Button = { Color = "0.1 0.1 0.1 0.3", Close = Layer },
                Text = { Text = "" }
            }, Layer);
			//Надпись
			container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0.83", AnchorMax = "1 1", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0" },
                Text = { Text = $"<size=29><b>ОБМЕН БОНУСОВ НА РУБЛИ В МАГАЗИН</b></size>\n<color=#e0e0e0>Здесь вы можете обменять накопленные бонусы XP на реальную валюты и вывести их в магазин.</color>", Color = "1 1 1 0.6313726", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 15 }
            }, Layer);
            //Инфо-блок возле аватарки №1
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.37617 0.5315456", AnchorMax = $"0.7635456 0.7725456" }, //Max 0-Шырина 0-Высота
                Button = { Color = "1 1 1 0.03", Material = "assets/content/ui/uibackgroundblur.mat" }, 
                Text = { Text = $"<size=17>    ЗДРАВСТВУЙ, <color=#a5e664><b>{player.displayName}</b></color></size>\n\n"+ Settings.GUISettings.MainBlockText, Color = HexToRustFormat("#e0e0e0"), Font = "robotocondensed-regular.ttf", FontSize = 16, Align = TextAnchor.MiddleLeft, FadeIn = 1f }
            }, Layer);
            //Инфо-блок ниже №2
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.2355456 0.3605456", AnchorMax = $"0.7635456 0.4335456" },
                Button = { Color = "0 0 0 0" }, 
                Text = { Text = Settings.GUISettings.SecondBlockText, Color = HexToRustFormat("#e0e0e0"), Font = "robotocondensed-regular.ttf", FontSize = 16, Align = TextAnchor.MiddleCenter, FadeIn = 1f }
            }, Layer);
            //Ваш баланс
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.2355456 0.210", AnchorMax = $"0.7635456 0.250" },
                Button = { Color = "0 0 0 0" },
                Text = { Text = $"ВАШ БАЛАНС <b>XP</b>: <color=#a5e664>{XPS[player.userID].XP}</color>", Color = HexToRustFormat("#e0e0e0"), Font = "robotocondensed-regular.ttf", FontSize = 20, Align = TextAnchor.MiddleCenter, FadeIn = 1f }
            }, Layer);
            //Аватарка до 415 строчки
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.235 0.5315456", AnchorMax = $"0.3725456 0.772" }, //Max 0-Шырина 0-Высота
                Button = { Color = "1 1 1 0.03", Material = "assets/content/ui/uibackgroundblur.mat" },
                Text = { Text = "", FadeIn = 1f }
            }, Layer, "Avatar");

            container.Add(new CuiElement
            {
                Parent = "Avatar",
                Components =
                {
                    new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", player.UserIDString) },
                    new CuiRectTransformComponent { AnchorMin = "0.02 0.0215", AnchorMax = "0.9795 0.980", OffsetMax = "0 0" }
                }
            });
			container.Add(new CuiButton //КНОПКА ОБМЕНА
            {
                RectTransform = { AnchorMin = "0.3875456 0.130", AnchorMax = "0.6115456 0.197" }, //Max 0-Шырина 0-Высота
                Button = { Color = "0.4 0.67 0.41 0.9", Command = "xpsell", Close = Layer, Material = "assets/content/ui/uibackgroundblur.mat" },
                Text = { Text = "ОБМЕНЯТЬ НА РУБЛИ", Align = TextAnchor.MiddleCenter, FontSize = 25, Font = "robotocondensed-regular.ttf", Color = HexToRustFormat("#e0e0e0") }
            }, Layer); 
			
			CuiHelper.AddUi(player, container);
		}
		
		#endregion

        #region Helpers [Хелперы]

		private static string HexToRustFormat(string hex)
        {
            if (string.IsNullOrEmpty(hex))
            {
                hex = "#FFFFFFFF";
            }

            var str = hex.Trim('#');

            if (str.Length == 6)
                str += "FF";

            if (str.Length != 8)
            {
                throw new Exception(hex);
                throw new InvalidOperationException("Cannot convert a wrong format.");
            }

            var r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber);
            var g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
            var b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);
            var a = byte.Parse(str.Substring(6, 2), NumberStyles.HexNumber);

            Color color = new Color32(r, g, b, a);

            return $"{color.r:F2} {color.g:F2} {color.b:F2} {color.a:F2}";
        }
		
        #endregion
    }
}