using Oxide.Core;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("UniqueCupboard", "https://topplugin.ru/", "2.1.0")]
    class UniqueCupboard : RustPlugin
    {
		#region Config
		
		private class Configuration
		{
			[JsonProperty("Время которое требуется отыграть на сервере, для получения уникального шкафа в минутах", Order = 0)]
			public int PlayedTime = 1;			
			[JsonProperty("Название уникального шкафа", Order = 1)]
			public string CupboardName = "Магический шкаф";
			[JsonProperty("Настройки команд", Order = 2)]
			public CommandSettings Command = new CommandSettings();	
			[JsonProperty("Приз", Order = 3)]
			public PrizeSettings Prize = new PrizeSettings();	
			[JsonProperty("Настройки очков", Order = 4)]
			public PointSettings Points = new PointSettings();	
			
			
			public class PointSettings{
				[JsonProperty("Сколько очков отнимать в случае потери своего шкафа", Order = 0)]
				public int takePoint = 50;
				
			}
			public class PrizeSettings{
				[JsonProperty("Исполняемая команда, для участника занявшего 1-ое место", Order = 0)]
				public string prize1 = "giveto {0} sulfur 10000";
				[JsonProperty("Исполняемая команда, для участника занявшего 2-ое место", Order = 0)]
				public string prize2 = "giveto {0} sulfur 5000";
				[JsonProperty("Исполняемая команда, для участника занявшего 3-ое место", Order = 0)]
				public string prize3 = "giveto {0} sulfur 2000";
			}
			
			public class CommandSettings{
				[JsonProperty("Команда доступа к статистике участников ивента", Order = 0)]
				public string topCommand = "toptc";
				[JsonProperty("Команда для участия в ивенте", Order = 1)]
				public string yesCommand = "yes";
				[JsonProperty("Команда для проверки наличия уникального шкафа", Order = 2)]
				public string whoCommand = "who";
			}		
			
			public static Configuration GetNewConfiguration(){
				Configuration newConfig = new Configuration();
				newConfig.PlayedTime=60;
				newConfig.CupboardName="Магический шкаф";
				newConfig.Command = new CommandSettings(){
					topCommand = "toptc",
					yesCommand = "yes",
					whoCommand = "who"
				};
				newConfig.Prize = new PrizeSettings()
				{
					prize1 = "giveto {0} sulfur 10000",
					prize2 = "giveto {0} sulfur 5000",
					prize3 = "giveto {0} sulfur 2000"
				};	
				newConfig.Points= new PointSettings(){
					takePoint = 50
				};
				return newConfig;		
			}
		}
		private Configuration config;
		
		protected override void LoadConfig()
		{
			base.LoadConfig();
			try
			{
				config = Config.ReadObject<Configuration>();
				if (config?.Prize == null) LoadDefaultConfig();
			}
			catch
			{
				LoadDefaultConfig();
			}
			NextTick(SaveConfig);
		}
		protected override void LoadDefaultConfig() => config = Configuration.GetNewConfiguration();
		protected override void SaveConfig() => Config.WriteObject(config);
		#endregion

        #region Variables

        public DataManager playerData { get; set; } = new DataManager();
        public Dictionary<ulong, CupboardData> cupboardsData { get; set; } = new Dictionary<ulong, CupboardData>();
        public Dictionary<uint, CupboardData> cupboardsDataOfUint { get; set; } = new Dictionary<uint, CupboardData>();
        public Dictionary<ulong, int> ActivePage { get; set; } = new Dictionary<ulong, int>();

        #endregion

        #region Classes

        public class CupboardData
        {
            public int PointsCount { get; set; } = 0;
            public uint NetId { get; set; } = 0;
            public DateTime PlacedTime { get; set; } = DateTime.Now;
            public string ownerName { get; set; }

            public void SetPoints(int points)
            {
                PointsCount += points;
            }

            public CupboardData(uint NetId, string displayName)
            {
                this.NetId = NetId;
                ownerName = displayName;
            }
        }

        public class DataManager
        {
            public DateTime WipeTime { get; set; } = DateTime.Now;
            public Dictionary<ulong, int> TimePlayed { get; set; } = new Dictionary<ulong, int>();
            public List<ulong> CupboardCreate { get; set; } = new List<ulong>();
			
			public void PrepareData(ulong userID){
				if (!this.TimePlayed.ContainsKey(userID)){
					this.TimePlayed.Add(userID,0);
				}
				return;
			}
        }

        #endregion

        #region Hooks

        private void OnServerInitialized()
        {
            LoadData();
			cmd.AddChatCommand(config.Command.whoCommand, this, cupboardWho);
			cmd.AddChatCommand(config.Command.yesCommand, this, cupboardYes);
			cmd.AddChatCommand(config.Command.topCommand, this, CMDtopTC);
            FillNetDataOfCupboards();
            TimeUpdate();
            
            foreach (var check in BasePlayer.activePlayerList)
                OnPlayerConnected(check);
        }

        void OnNewSave(string filename)
        {
            PrintWarning("Обнаружен вайп, происходит выдача призов за победу в ивенте -Уникальный Шкаф- и очистка даты!");

            var playersTop = GetListTopOfPage(1);
            Server.Command(config.Prize.prize1, playersTop[0]);
            Server.Command(config.Prize.prize2, playersTop[1]);
            Server.Command(config.Prize.prize3, playersTop[2]);

            playerData = new DataManager();
            cupboardsData = new Dictionary<ulong, CupboardData>();
            SaveData();
        }

		//При сохранении данных	
		void OnServerSave()=>SaveData();
        private void Unload()=>SaveData();		

        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (!(entity is BuildingPrivlidge) || !cupboardsDataOfUint.ContainsKey(entity.net.ID)) return;

            cupboardsDataOfUint[entity.net.ID].PointsCount -= config.Points.takePoint;
            if (info.InitiatorPlayer == null || IsYourCupboard(info.InitiatorPlayer.userID, entity.net.ID)) return;
            var pointResult = CalculatePoints(entity as BuildingPrivlidge);
            if (BasePlayer.FindByID(entity.OwnerID) == null) pointResult /= 2;
            if (!cupboardsData.ContainsKey(info.InitiatorPlayer.userID)) return;
            cupboardsData[info.InitiatorPlayer.userID].SetPoints(pointResult);
			info.InitiatorPlayer.ChatMessage($"Вы получили {pointResult} поинтов.\nПросмотреть рейтинг топа можно через команду /{config.Command.topCommand}");
        }

        object CanBuild(Planner planner, Construction prefab, Construction.Target target)
        {
            if (planner.GetItem().info.itemid != -97956382 || planner.GetItem().skin != 1 || planner.GetItem()?.name != config.CupboardName) return null;

            if (!playerData.CupboardCreate.Contains(planner.GetOwnerPlayer().userID) || !HasPriviligesToUseCupboard(planner.GetOwnerPlayer()))
            {
               return false;
            }

            return null;
        }

        private void OnEntityBuilt(Planner plan, GameObject go)
        {
            BaseEntity baseEntity = go.ToBaseEntity();

            if (!(baseEntity is BuildingPrivlidge)) return;
            if (baseEntity.skinID == 0) return;

            if (!playerData.CupboardCreate.Contains(plan.GetOwnerPlayer().userID) || !HasPriviligesToUseCupboard(plan.GetOwnerPlayer()))
            {
                return;
            }

            cupboardsData.Add(plan.GetOwnerPlayer().userID, new CupboardData(go.ToBaseEntity().net.ID, plan.GetOwnerPlayer().displayName));
            cupboardsDataOfUint.Add(go.ToBaseEntity().net.ID, cupboardsData[plan.GetOwnerPlayer().userID]);
        }

        #endregion

        #region Data

        private void LoadData()
        {
            playerData = Interface.Oxide.DataFileSystem.ReadObject<DataManager>("UniqueCupboardData/PlayersData");
            cupboardsData = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, CupboardData>>("UniqueCupboardData/CupboardsData");

            if (playerData == null) playerData = new DataManager();
            if (cupboardsData == null) cupboardsData = new Dictionary<ulong, CupboardData>();
        }

        private void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject("UniqueCupboardData/PlayersData", playerData);
            Interface.Oxide.DataFileSystem.WriteObject("UniqueCupboardData/CupboardsData", cupboardsData);
        }

        #endregion

        #region Commands

        [ConsoleCommand("CUPBOARD_CLOSE")]
        private void CloseCupboard(ConsoleSystem.Arg arg)
        {
            if (arg.Connection == null) return;
            CuiHelper.DestroyUi(arg.Player(), "Oснова");
        }

        [ConsoleCommand("CUPBOARD_NEXTPAGE")]
        private void CupboardNextPage(ConsoleSystem.Arg arg)
        {
            if (arg.Connection == null) return;
            if (cupboardsData.Count <= 6) return;

            int pages = (int)Math.Ceiling((double)cupboardsData.Count / 6);

            if (ActivePage.ContainsKey(arg.Player().userID))
            {
                if(ActivePage[arg.Player().userID] + 1 <= pages)
                GUIShowTCTOP(arg.Player(), ActivePage[arg.Player().userID] + 1);
            }
            else GUIShowTCTOP(arg.Player(), 1);
        }

        [ConsoleCommand("CUPBOARD_BackPage")]
        private void CupboardBackPage(ConsoleSystem.Arg arg)
        {
            if (arg.Connection == null) return;
            if (ActivePage.ContainsKey(arg.Player().userID))
                GUIShowTCTOP(arg.Player(), ActivePage[arg.Player().userID] - 1);
            else GUIShowTCTOP(arg.Player(), 1);
        }

        private void CMDtopTC(BasePlayer player, string command, string[] args)
        {
            GUIShowTCTOP(player, 1);
        }

        private void cupboardWho(BasePlayer player, string command, string[] args)
        {
            if (!player.IsBuildingAuthed() && !player.IsBuildingBlocked()) {
                player.ChatMessage("<color=#DB7093>Уникальный шкаф не обнаружен!</color>");
                return; 
            }

            if (cupboardsDataOfUint.ContainsKey(player.GetBuildingPrivilege().net.ID))
            {
                int Points = CalculatePoints(player.GetBuildingPrivilege());
                player.ChatMessage($"Обнаружен уникальный шкаф. За него вы получите: {Points} поинт(ов)");
            }
        }

        private void cupboardYes(BasePlayer player, string command, string[] args)
        {
            if (playerData.CupboardCreate.Contains(player.userID))
            {
                player.ChatMessage($"<color=#DB7093>Вы уже получали уникальный шкаф!</color><size=7529></size>");
                return;
            }
			playerData.PrepareData(player.userID);			
			if (!HasPriviligesToUseCupboard(player)) 
			{
				string difTime = FormatShortTime(config.PlayedTime-playerData.TimePlayed[player.userID]);
				player.ChatMessage($"Для доступа к соревнованиям вам осталось сыграть на сервере {difTime}"); 
				return; 
			} 
            playerData.CupboardCreate.Add(player.userID);
			player.ChatMessage($"Вы получили <color=#DB7093>{config.CupboardName}</color>.\nРазместите его в укрекпленном здании и не дайте другим игрока до него добраться!\nРейтинг участников ивента вы можете посмотреть через команду <color=#DB7093>/{config.Command.topCommand}</color>");
            player.GiveItem(GetCupboardItem());
        }

        #endregion

        private bool IsNPC(BasePlayer player)
        {
            if (player == null) return false;
            if (player is NPCPlayer)
                return true;
            if (!(player.userID >= 76560000000000000L || player.userID <= 0L))
                return true;
            return false;
        }

        void OnPlayerConnected(BasePlayer player)
        {            
            if (player==null) return;
			if (IsNPC(player)) return;
			SendReply(player, $"Вы можете принять участие в соревнованиях <color=#DB7093>Уникальный шкаф</color>\nДля участия вам необходимо получить Уникальный шкаф.\nПодробнее вы можете узнать через команду <color=#DB7093>/toptc</color>.\nДля получения шкафа выполните команду <color=#DB7093>/yes</color>");  
        }
        #region Functions

        private void GUIShowTCTOP(BasePlayer player, int page)
        {
            CuiHelper.DestroyUi(player, "Oснова");
            string GUITop = "[{\"name\":\"Oснова\",\"parent\":\"Overlay\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0.282353 0.2705882 0.2431373 0.6194041\"},{\"type\":\"NeedsCursor\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.1541667 0.137037\",\"anchormax\":\"0.8520834 0.8583333\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Уникальный шкаф\",\"parent\":\"Oснова\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0.5215687 0.5137255 0.509804 0.6227752\"},{\"type\":\"RectTransform\",\"anchormin\":\"-1.305016E-07 0.89729\",\"anchormax\":\"0.9999999 1.000141\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"надпись\",\"parent\":\"Уникальный шкаф\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"УНИКАЛЬНЫЙ ШКАФ\",\"fontSize\":15,\"align\":\"MiddleCenter\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.4119403 0.08785719\",\"anchormax\":\"0.5873134 0.890101\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Aктивных\",\"parent\":\"Oснова\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0.3411765 0.3176471 0.2666667 0.6227756\"},{\"type\":\"RectTransform\",\"anchormin\":\"4.202593E-08 2.30968E-07\",\"anchormax\":\"0.9999993 0.1051915\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"надпись\",\"parent\":\"Aктивных\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0.4039216 0.3803922 0.3411765 0.6227756\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.4000001 0.08590239\",\"anchormax\":\"0.5992543 0.927461\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"AKТ1BHЫХ\",\"parent\":\"надпись\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"АКТИВНЫХ:\",\"fontSize\":13,\"align\":\"MiddleLeft\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.2589047 0.08757434\",\"anchormax\":\"0.7415726 0.8694906\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"COUNT\",\"parent\":\"надпись\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"%COUNT%\",\"fontSize\":13,\"align\":\"MiddleLeft\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.7003741 0.1600794\",\"anchormax\":\"0.8913847 0.7836232\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"▶\",\"parent\":\"Aктивных\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"▶\",\"fontSize\":36,\"align\":\"MiddleCenter\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.951483 0.08590239\",\"anchormax\":\"0.9962591 0.927461\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"BUTTON\",\"parent\":\"▶\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"command\":\"CUPBOARD_NEXTPAGE\",\"color\":\"1 1 1 0\"},{\"type\":\"RectTransform\",\"anchormin\":\"2.980232E-08 0.0005682539\",\"anchormax\":\"1 1.000568\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"3aпись\",\"parent\":\"Oснова\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"• Уничтожьте как можно больше уникальных шкафов чтобы попасть в топ.\n• Чтобы узнать сколько очков Вы получите за уничтожение уникального шкафа, подойдите к дому на расстояние не менее 15м, наведите на него\nприцел и введите команду /"+config.Command.whoCommand+". Если ваш шкаф уничтожат, вы потеряете очки.\n• Количество получаемых очков зависит от прошедших дней после вайпа.\n• Время с момента вайпа и количество получаемых очков:\n• 1 день - 0 очков, 2 день - 2 очка, 3 день - 6 очков, 4 день - 10 очков, 5 день - 14 очков, 6 день - 18 очков, 7 день - 22 очков.\n• Игрок выбывает из ивента при получении бана или уничтожении шкафа друга или абуза очков путем продажи/отдачи шкафа для намеренного\nполучения очков.\",\"fontSize\":13},{\"type\":\"RectTransform\",\"anchormin\":\"0.01343272 0.5752797\",\"anchormax\":\"0.9895521 0.886486\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"1\",\"parent\":\"Oснова\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0.2784314 0.2666667 0.227451 0.6227751\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.009104644 0.5040274\",\"anchormax\":\"0.9908953 0.568698\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Цифра\",\"parent\":\"1\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"1\",\"align\":\"MiddleCenter\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.002888201 0.06674828\",\"anchormax\":\"0.03177237 0.9202884\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"ник\",\"parent\":\"1\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"%NICK1%\",\"align\":\"MiddleLeft\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.05792049 0.1025348\",\"anchormax\":\"0.278048 0.9163762\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Очки\",\"parent\":\"1\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"Очки:\",\"align\":\"MiddleCenter\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.7754731 0.1025348\",\"anchormax\":\"0.8514811 0.9163762\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"SCORE1\",\"parent\":\"1\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"%SCORE1%\",\"align\":\"MiddleLeft\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.8355241 0.08268505\",\"anchormax\":\"0.8802736 0.936226\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"2\",\"parent\":\"Oснова\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0.2784314 0.2666667 0.227451 0.6227756\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.009104642 0.4273805\",\"anchormax\":\"0.9908953 0.4920527\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Цифра\",\"parent\":\"2\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"2\",\"align\":\"MiddleCenter\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.002888201 0.06674666\",\"anchormax\":\"0.03177238 0.9202661\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"ник\",\"parent\":\"2\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"%NICK2%\",\"align\":\"MiddleLeft\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.05792049 0.1025322\",\"anchormax\":\"0.278048 0.916353\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Очки\",\"parent\":\"2\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"Очки\",\"align\":\"MiddleCenter\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.7754731 0.1025322\",\"anchormax\":\"0.8514811 0.916353\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"SCORE2\",\"parent\":\"2\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"%SCORE2%\",\"align\":\"MiddleLeft\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.8355241 0.08268295\",\"anchormax\":\"0.8802736 0.9362024\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"3\",\"parent\":\"Oснова\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0.2784314 0.2666667 0.227451 0.6227756\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.009104642 0.350734\",\"anchormax\":\"0.9908953 0.4154047\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Цифра\",\"parent\":\"3\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"3\",\"align\":\"MiddleCenter\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.002888201 0.06674819\",\"anchormax\":\"0.03177238 0.9202872\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"ник\",\"parent\":\"3\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"%NICK3%\",\"align\":\"MiddleLeft\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.05792049 0.1025346\",\"anchormax\":\"0.278048 0.9163745\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Очки\",\"parent\":\"3\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"Очки:\",\"align\":\"MiddleCenter\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.7754731 0.1025346\",\"anchormax\":\"0.8514811 0.9163745\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"SCORE3\",\"parent\":\"3\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"%SCORE3%\",\"align\":\"MiddleLeft\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.8355241 0.08268486\",\"anchormax\":\"0.8802736 0.9362239\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"4\",\"parent\":\"Oснова\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0.2784314 0.2666667 0.227451 0.6227752\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.009104642 0.2740873\",\"anchormax\":\"0.9908953 0.338758\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Цифра\",\"parent\":\"4\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"4\",\"align\":\"MiddleCenter\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.002888201 0.06674819\",\"anchormax\":\"0.03177238 0.9202872\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"ник\",\"parent\":\"4\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"%NICK4%\",\"align\":\"MiddleLeft\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.05792049 0.1025346\",\"anchormax\":\"0.278048 0.9163746\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Очки\",\"parent\":\"4\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"Очки:\",\"align\":\"MiddleCenter\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.7754731 0.1025346\",\"anchormax\":\"0.8514811 0.9163746\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"SCORE4\",\"parent\":\"4\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"%SCORE4%\",\"align\":\"MiddleLeft\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.8355241 0.08268489\",\"anchormax\":\"0.8802736 0.9362243\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"5\",\"parent\":\"Oснова\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0.2784314 0.2666667 0.227451 0.6227751\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.009104642 0.1974406\",\"anchormax\":\"0.9908953 0.2621113\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Цифра\",\"parent\":\"5\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"5\",\"align\":\"MiddleCenter\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.002888201 0.06674821\",\"anchormax\":\"0.03177238 0.9202872\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"ник\",\"parent\":\"5\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"%NICK5%\",\"align\":\"MiddleLeft\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.05792049 0.1025346\",\"anchormax\":\"0.278048 0.916374\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Очки\",\"parent\":\"5\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"Очки:\",\"align\":\"MiddleCenter\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.7754731 0.1025346\",\"anchormax\":\"0.8514811 0.9163743\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"SCORE5\",\"parent\":\"5\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"%SCORE5%\",\"align\":\"MiddleLeft\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.8355241 0.08268486\",\"anchormax\":\"0.8802736 0.9362239\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"6\",\"parent\":\"Oснова\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0.2784314 0.2666667 0.227451 0.6227756\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.009104642 0.1195963\",\"anchormax\":\"0.9908953 0.1842669\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Цифра\",\"parent\":\"6\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"6\",\"align\":\"MiddleCenter\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.002888201 0.06674831\",\"anchormax\":\"0.03177238 0.9202887\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"ник\",\"parent\":\"6\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"%NICK6%\",\"align\":\"MiddleLeft\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.05792049 0.1025348\",\"anchormax\":\"0.2780479 0.9163759\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Очки\",\"parent\":\"6\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"Очки:\",\"align\":\"MiddleCenter\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.7754735 0.1025348\",\"anchormax\":\"0.8514814 0.916376\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"SCORE6\",\"parent\":\"6\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"%SCORE6%\",\"align\":\"MiddleLeft\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.835524 0.08268501\",\"anchormax\":\"0.8802735 0.9362256\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"1\",\"parent\":\"Oснова\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"command\":\"CUPBOARD_CLOSE\",\"color\":\"1 1 1 0\"},{\"type\":\"RectTransform\",\"anchormin\":\"-0.2208956 -0.1899871\",\"anchormax\":\"-0.001492575 1.195122\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"2\",\"parent\":\"Oснова\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"command\":\"CUPBOARD_CLOSE\",\"color\":\"1 1 1 0\"},{\"type\":\"RectTransform\",\"anchormin\":\"-0.2208955 1\",\"anchormax\":\"1.211194 1.195122\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"3\",\"parent\":\"Oснова\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"command\":\"CUPBOARD_CLOSE\",\"color\":\"1 1 1 0\"},{\"type\":\"RectTransform\",\"anchormin\":\"1.001492 -0.1899871\",\"anchormax\":\"1.210448 1.007702\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"4\",\"parent\":\"Oснова\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"command\":\"CUPBOARD_CLOSE\",\"color\":\"1 1 1 0\"},{\"type\":\"RectTransform\",\"anchormin\":\"-0.1231344 -0.1899871\",\"anchormax\":\"1.211194 -0.001283646\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"CuiElement\",\"parent\":\"Overlay\",\"components\":[{\"type\":\"RectTransform\",\"anchormin\":\"0.05208334 0.09259259\",\"anchormax\":\"0.1041667 0.1851852\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]}]";

            int playerPosition = 1;
            foreach (var cupboard in GetListTopOfPage(page))
            {
                string nameofPlayer = "";
                int scoreOfPlayer = 0;

                if (cupboard != 0)
                {
                    nameofPlayer = cupboardsData[cupboard].ownerName;
                    scoreOfPlayer = cupboardsData[cupboard].PointsCount;
                }

                GUITop = GUITop.Replace($"%NICK{playerPosition}%", nameofPlayer)
                    .Replace($"%SCORE{playerPosition}%", scoreOfPlayer.ToString());

                playerPosition++;
            }

            GUITop = GUITop.Replace("%COUNT%", cupboardsData.Count.ToString());
            if (ActivePage.ContainsKey(player.userID)) ActivePage[player.userID] = page;
            else ActivePage.Add(player.userID, page);

            CuiHelper.AddUi(player, GUITop);
        }

        private List<ulong> GetListTopOfPage(int Page)
        {
            int maxCount = Page * 6;
            int startCount = maxCount - 6;

            var sortUnqieuCupboards = from cupboard in cupboardsData orderby cupboard.Value.PointsCount descending select cupboard;

            List<ulong> playersTop = new List<ulong>();

            for (int i = startCount; i < maxCount; i++)
            {
                if (sortUnqieuCupboards.Count() - 1 < i) playersTop.Add(0);
                else
                {
                    playersTop.Add(sortUnqieuCupboards.ElementAt(i).Key);
                }
            }

            return playersTop;
        }

        private void TimeUpdate()
        {
            timer.Repeat(1 * 60f, 0, () => {
               var playerList = BasePlayer.activePlayerList.Where(x => (playerData.TimePlayed.ContainsKey(x.userID) && playerData.TimePlayed[x.userID] < config.PlayedTime) || !playerData.TimePlayed.ContainsKey(x.userID)).ToList();
                foreach(var player in playerList)
                {
                    if (!playerData.TimePlayed.ContainsKey(player.userID))
                        playerData.TimePlayed.Add(player.userID, 1);
                    else
                    {
                        playerData.TimePlayed[player.userID] += 1;
                    }
                }
            });
        }

        private int CalculatePointsOfWipe(DateTime cupboardTime)
        {
            int daysWipe = (int)DateTime.Now.Subtract(cupboardTime).TotalDays;
			PrintWarning("CalculatePointsOfWipe");
			Puts($"daysWipe {daysWipe}");
            if (daysWipe == 0) return 0;

            int totalPoints = 0;
            for(int i = 2; i < daysWipe; i++)
            {
				Puts($"i {i}");
                if (i == 2)
                {
                    totalPoints = 2;
                    continue;
                }

                if (i == 3) {
                    totalPoints = 3;
                    continue;
                }

                totalPoints *= 2;

            }
			Puts($"totalPoints {totalPoints}");
            return totalPoints;
        }

        private int CalculateOldPoints(BuildingPrivlidge cupboard)
        {
			Puts($"cupboard.CalculateBuildingTaxRate() = {cupboard.CalculateBuildingTaxRate()}");
            int addPrice = (int)(cupboard.CalculateBuildingTaxRate() * 10);
			Puts($"cupboardsDataOfUint = {cupboardsDataOfUint[cupboard.net.ID].PlacedTime}");
			Puts($"CalculatePointsOfWipe = {CalculatePointsOfWipe(cupboardsDataOfUint[cupboard.net.ID].PlacedTime)}");
            addPrice += CalculatePointsOfWipe(cupboardsDataOfUint[cupboard.net.ID].PlacedTime);
			Puts($"addPrice = {addPrice}");
            return addPrice;
        }

        private int CalculatePoints(BuildingPrivlidge cupboard)
        {
            int addPrice=0;
			Puts($"placed {cupboardsDataOfUint[cupboard.net.ID].PlacedTime}");
			int daysWipe = (int)DateTime.Now.Subtract(playerData.WipeTime).TotalDays;
			Puts($"day {daysWipe}");
			if (daysWipe==0) return 0;
            //addPrice = (int)Math.Pow((daysWipe-1),2);
			addPrice = ((daysWipe-1)+(daysWipe))*2;
			Puts($"daysWipe = {addPrice}");
            return addPrice;
        }
		
        private bool IsYourCupboard(ulong initiatorId, uint netid) => cupboardsData.ContainsKey(initiatorId) && cupboardsData[initiatorId].NetId == netid;

        private void FillNetDataOfCupboards()
        {
            foreach (var CupboardData in cupboardsData)
                cupboardsDataOfUint.Add(CupboardData.Value.NetId, CupboardData.Value);
        }

		public static string FormatShortTime(int min) 
		{
			TimeSpan time = TimeSpan.FromSeconds(min*60);
			string result = string.Empty;
			if (time.Hours>0) result += $"{time.Hours} ч. ";
			if (time.Minutes>0) result += $"{time.Minutes} мин. ";
			if (string.IsNullOrEmpty(result)) result ="0";
			return result;
		}	
        private bool HasPriviligesToUseCupboard(BasePlayer player)
        {
            if (playerData.TimePlayed.ContainsKey(player.userID))
				if (playerData.TimePlayed[player.userID] >= config.PlayedTime)
					return true;
            return false;            
        }

        private Item GetCupboardItem()
        {
            var item =  ItemManager.CreateByItemID(-97956382, 1, 1);
            item.name = config.CupboardName;
            return item;
        }

        #endregion

    }
}
