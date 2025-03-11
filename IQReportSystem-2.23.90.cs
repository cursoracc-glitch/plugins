using Oxide.Game.Rust.Cui;
using System.Text;
using System.Collections.Generic;
using Oxide.Core.Libraries;
using Object = System.Object;
using ConVar;
using Newtonsoft.Json;
using Facepunch.Utility;
using System.Linq;
using Network;
using Oxide.Core;
using Net = Network.Net;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using Oxide.Core.Plugins;
using UnityEngine.Networking;
using System.Collections;
using Newtonsoft.Json.Linq;
using Oxide.Core.Libraries.Covalence;
using System;
using Time = UnityEngine.Time;

namespace Oxide.Plugins
{
    /* Плагин */
    [Info("IQReportSystem", "Mercury", "2.23.90")]
    [Description("One love IQReportSystem")]
    internal class IQReportSystem : RustPlugin
    {
		private Boolean IsClans(String userID, String targetID)
		{
			if (!Clans) return false;
			if (!Clans.Author.Contains("dcode"))
			{
				Object result = Clans.Call("IsClanMember", userID, targetID);
				if (result == null)
					return false;
				
				return (Boolean)result;
			}
			String TagUserID = (String)Clans.Call("GetClanOf", userID);
			String TagTargetID = (String)Clans.Call("GetClanOf", targetID);
			return TagUserID == TagTargetID;
		}
                
        
        private class ImageUi
        {
            private static Coroutine coroutineImg = null;
			private static Dictionary<String, String> Images = new Dictionary<String, String>();

			private static List<String> KeyImages = new List<String>();

			public static void DownloadImages() { coroutineImg = ServerMgr.Instance.StartCoroutine(AddImage()); }

			private static IEnumerator AddImage()
			{
				if (_ == null)
					yield break;
				_.PrintWarning("Генерируем интерфейс, ожидайте ~10-15 секунд!");
				foreach (String URL in KeyImages)
				{
					String KeyName = URL;
					if (KeyName == null) throw new ArgumentNullException(nameof(KeyName));

					UnityWebRequest www = UnityWebRequestTexture.GetTexture(URL);
					yield return www.SendWebRequest();

					if (www.isNetworkError || www.isHttpError)
					{
						_.PrintWarning($"Image download error! Error: {www.error}, Image name: {KeyName}");
						www.Dispose();
						coroutineImg = null;
						yield break;
					}

					Texture2D texture = DownloadHandlerTexture.GetContent(www);
					if (texture != null)
					{
						Byte[] bytes = texture.EncodeToPNG();

						String image = FileStorage.server.Store(bytes, FileStorage.Type.png, CommunityEntity.ServerInstance.net.ID).ToString();
						if (!Images.ContainsKey(KeyName))
							Images.Add(KeyName, image);
						else
							Images[KeyName] = image;

						UnityEngine.Object.DestroyImmediate(texture);
					}

					www.Dispose();
					yield return CoroutineEx.waitForSeconds(0.02f);
				}
		   		 		  						  	   		  	   		  	 				   		 		  				
				yield return CoroutineEx.waitForSeconds(0.02f);
				coroutineImg = null;

				_interface = new InterfaceBuilder();
				_.PrintWarning("Интерфейс успешно загружен!");
			}

            public static string GetImage(String ImgKey)
            {
                if (Images.ContainsKey(ImgKey))
                    return Images[ImgKey];
                return _.GetImage("LOADING");
            }
			public static void Initialize()
			{
				KeyImages = new List<String>();
				Images = new Dictionary<string, string>();

                Configuration.Images ImagesPlugin = config.ImagesSettings;
		   		 		  						  	   		  	   		  	 				   		 		  				
                                if (!KeyImages.Contains(ImagesPlugin.Background))
                    KeyImages.Add(ImagesPlugin.Background);
		   		 		  						  	   		  	   		  	 				   		 		  				
				if (!KeyImages.Contains(ImagesPlugin.PageDown))
					KeyImages.Add(ImagesPlugin.PageDown);

				if (!KeyImages.Contains(ImagesPlugin.PageUp))
					KeyImages.Add(ImagesPlugin.PageUp);		
				
				if (!KeyImages.Contains(ImagesPlugin.Search))
					KeyImages.Add(ImagesPlugin.Search);
				
				if (!KeyImages.Contains(ImagesPlugin.AvatarBlur))
					KeyImages.Add(ImagesPlugin.AvatarBlur);

								
				if (!KeyImages.Contains(ImagesPlugin.ReasonModeratorAndRaiting))
					KeyImages.Add(ImagesPlugin.ReasonModeratorAndRaiting);
				
				if (!KeyImages.Contains(ImagesPlugin.PlayerAlerts))
					KeyImages.Add(ImagesPlugin.PlayerAlerts);

				
				if (!KeyImages.Contains(ImagesPlugin.StatisticsBlockSettings.BlockStatsModeration))
					KeyImages.Add(ImagesPlugin.StatisticsBlockSettings.BlockStatsModeration);		
				
				if (!KeyImages.Contains(ImagesPlugin.StatisticsBlockSettings.BlockStatsRaitingModeration))
					KeyImages.Add(ImagesPlugin.StatisticsBlockSettings.BlockStatsRaitingModeration);
				
				if (!KeyImages.Contains(ImagesPlugin.StatisticsBlockSettings.RaitingImage))
					KeyImages.Add(ImagesPlugin.StatisticsBlockSettings.RaitingImage);
		   		 		  						  	   		  	   		  	 				   		 		  				
				
				
				if (!KeyImages.Contains(ImagesPlugin.PlayerListBlockSettings.PoopUpBackgorund))
					KeyImages.Add(ImagesPlugin.PlayerListBlockSettings.PoopUpBackgorund);
				
				if (!KeyImages.Contains(ImagesPlugin.PlayerListBlockSettings.PoopUpReasonBackgorund))
					KeyImages.Add(ImagesPlugin.PlayerListBlockSettings.PoopUpReasonBackgorund);

				
				
				if (!KeyImages.Contains(ImagesPlugin.LeftBlockSettings.ButtonBackgorund))
					KeyImages.Add(ImagesPlugin.LeftBlockSettings.ButtonBackgorund);
				
				if (!KeyImages.Contains(ImagesPlugin.LeftBlockSettings.ModerationIcon))
					KeyImages.Add(ImagesPlugin.LeftBlockSettings.ModerationIcon);
				
				if (!KeyImages.Contains(ImagesPlugin.LeftBlockSettings.ReportIcon))
					KeyImages.Add(ImagesPlugin.LeftBlockSettings.ReportIcon);

				
				
				if (!KeyImages.Contains(ImagesPlugin.ModerationBlockSettings.ModeratorPoopUPBackgorund))
					KeyImages.Add(ImagesPlugin.ModerationBlockSettings.ModeratorPoopUPBackgorund);
				
				if (!KeyImages.Contains(ImagesPlugin.ModerationBlockSettings.ModeratorPoopUPTextBackgorund))
					KeyImages.Add(ImagesPlugin.ModerationBlockSettings.ModeratorPoopUPTextBackgorund);
				
				if (!KeyImages.Contains(ImagesPlugin.ModerationBlockSettings.ModeratorPoopUPPanelBackgorund))
					KeyImages.Add(ImagesPlugin.ModerationBlockSettings.ModeratorPoopUPPanelBackgorund);

				
				
				if (!KeyImages.Contains(ImagesPlugin.ModeratorMenuCheckedSettings.ModeratorCheckedBackground))
					KeyImages.Add(ImagesPlugin.ModeratorMenuCheckedSettings.ModeratorCheckedBackground);
				
				if (!KeyImages.Contains(ImagesPlugin.ModeratorMenuCheckedSettings.ModeratorCheckedStopButton))
					KeyImages.Add(ImagesPlugin.ModeratorMenuCheckedSettings.ModeratorCheckedStopButton);
					
				if (!KeyImages.Contains(ImagesPlugin.ModeratorMenuCheckedSettings.ModeratorVerdictButton))
					KeyImages.Add(ImagesPlugin.ModeratorMenuCheckedSettings.ModeratorVerdictButton);

				if (!KeyImages.Contains(ImagesPlugin.ModeratorMenuCheckedSettings.SteamIcoPlayer))
					KeyImages.Add(ImagesPlugin.ModeratorMenuCheckedSettings.SteamIcoPlayer);
				
				if (!KeyImages.Contains(ImagesPlugin.ModeratorMenuCheckedSettings.PirateIcoPlayer))
					KeyImages.Add(ImagesPlugin.ModeratorMenuCheckedSettings.PirateIcoPlayer);
				
				
				if (!KeyImages.Contains(ImagesPlugin.PlayerMenuRaitingSettings.PlayerMenuRaitingBackground))
					KeyImages.Add(ImagesPlugin.PlayerMenuRaitingSettings.PlayerMenuRaitingBackground);

				
			}
            public static void Unload()
            {
                coroutineImg = null;
                foreach (KeyValuePair<String, String> item in Images)
                    FileStorage.server.RemoveExact(UInt32.Parse(item.Value), FileStorage.Type.png, CommunityEntity.ServerInstance.net.ID, 0U);

				KeyImages.Clear();
				KeyImages = null;
				Images.Clear();
				Images = null;
            }
        }
		
		
		private Boolean CanStartedChecked(BasePlayer Target) => IsValidStartChecked(Target, null);

        
        [ConsoleCommand("iqrs")]
        private void ConsoleCommandReport(ConsoleSystem.Arg arg) ///iqrs call SteamID
        {
	        if (!arg.HasArgs(1)) return;
	        String Actions = arg.Args[0];
	        BasePlayer player = arg.Player();
	        if (player != null)
		        if (!IsModerator(player))
			        return;

	        Boolean IsConsole = player == null;
	        BasePlayer Target = null;
	        UInt64 UserIDPlayer = 0;
	        if (!Actions.Contains("report"))
	        {
		        if (!arg.HasArgs(2)) return;
		        String NameOrID = arg.Args[1];
		        UserIDPlayer = Convert.ToUInt64(NameOrID.IsSteamId() ? NameOrID : covalence.Players.FindPlayer(NameOrID)?.Id);
		        Target = BasePlayer.Find(NameOrID);

		        if (Target == null)
		        {
			        if (!Actions.Contains("dismiss"))
			        {
				        if (player != null)
					        player.ConsoleMessage(LanguageEn
						        ? "The player is not on the server or you have entered incorrect data"
						        : "Игрока нет на сервере или вы указали неверные данные");
				        else
					        Puts(LanguageEn
						        ? "The player is not on the server or you have entered incorrect data"
						        : "Игрока нет на сервере или вы указали неверные данные");
				        return;
			        }
		        }
	        }

	        switch (Actions) 
	        {
		        case "give":
		        {
			        if (!arg.HasArgs(3)) return;
			        if (!IsConsole && !player.IsAdmin) return;
			        Int32 AmountGive;
			        if (!Int32.TryParse(arg.Args[2], out AmountGive))
			        {
				        PrintWarning(LanguageEn ? "You have entered a letter as quantity! Only numbers are supported" : "Вы ввели в качестве количества буквы! Поддерживаются только цифры");
				        return;
			        }
		   		 		  						  	   		  	   		  	 				   		 		  				
			        if (AmountGive <= 0)
			        {
				        PrintWarning(LanguageEn ? "Value cannot be less than or equal to zero." : "Значение не может быть меньше или равно нулю");
				        return;
			        }
			        
			        if (!PlayerInformations.ContainsKey(UserIDPlayer))
			        {
				        PrintWarning(LanguageEn ? "The player is not in the complaint system database" : "Игрока нет в базе данных системы жалоб");
				        return;
			        }

			        PlayerInformations[UserIDPlayer].Reports += AmountGive;
			        Puts(LanguageEn ? $"Player {UserIDPlayer} has been successfully awarded {AmountGive} reports. Total amount: {PlayerInformations[UserIDPlayer].Reports}" : $"Игроку {UserIDPlayer} успешно было начислено {AmountGive} репортов. Общее количество {PlayerInformations[UserIDPlayer].Reports}");
			        break;
		        }
		        case "remove":
		        {
			        if (!arg.HasArgs(3)) return;
			        if (!IsConsole && !player.IsAdmin) return;
			        Int32 AmountRemove;
			        if (!Int32.TryParse(arg.Args[2], out AmountRemove))
			        {
				        PrintWarning(LanguageEn ? "You have entered a letter as quantity! Only numbers are supported" : "Вы ввели в качестве количества буквы! Поддерживаются только цифры");
				        return;
			        }

			        if (AmountRemove <= 0)
			        {
				        PrintWarning(LanguageEn ? "Value cannot be less than or equal to zero." : "Значение не может быть меньше или равно нулю");
				        return;
			        }

			        if (!PlayerInformations.ContainsKey(UserIDPlayer))
			        {
				        PrintWarning(LanguageEn ? "The player is not in the complaint system database" : "Игрока нет в базе данных системы жалоб");
				        return;
			        }

			        PlayerInformations[UserIDPlayer].Reports -= PlayerInformations[UserIDPlayer].Reports <= AmountRemove ? PlayerInformations[UserIDPlayer].Reports : AmountRemove;
			        Puts(LanguageEn ? $"Player {UserIDPlayer} has been successfully remove {AmountRemove} reports. Total amount: {PlayerInformations[UserIDPlayer].Reports}" : $"Игроку {UserIDPlayer} успешно было удалено {AmountRemove} репортов. Общее количество {PlayerInformations[UserIDPlayer].Reports}");
			        break;
		        }
		        case "call":
		        {
			        if (!IsConsole && (!player.IsAdmin || !IsModerator(player))) return; 
			        
			        if (AfkCheckRoutine.ContainsKey(Target))
			        {
				        if (AfkCheckRoutine[Target] != null)
				        {
					        ServerMgr.Instance.StopCoroutine(AfkCheckRoutine[Target]);
					        AfkCheckRoutine[Target] = null;
					        
					        if(PlayerChecks.ContainsKey(Target.userID))
						        PlayerChecks.Remove(Target.userID);
				        }
			        }
			        
			        Coroutine routineAFK = ServerMgr.Instance.StartCoroutine(StartAfkCheck(Target, player, IsConsole, !config.CheckControllerSettings.UseCheckAFK));

			        if (!AfkCheckRoutine.ContainsKey(Target))
				        AfkCheckRoutine.Add(Target, routineAFK);
			        else AfkCheckRoutine[Target] = routineAFK;
			        
			        break;
		        }
		        case "dismiss":
		        {
			        if (!IsConsole && (!player.IsAdmin || !IsModerator(player))) return; 

			        StopCheckedPlayer(UserIDPlayer, player, IsConsole: IsConsole);
			        break;
		        }
		        case "report.list": 
		        case "reports":
		        {
			        if (!IsConsole && !player.IsAdmin) return;
			        
			        IOrderedEnumerable<BasePlayer> moderatorSortedList = BasePlayer.activePlayerList
				        .Where(x => PlayerInformations[x.userID].Reports >=
				                    config.ReportContollerModerationSettings.ReportCountTrigger)
				        .OrderByDescending(x => PlayerInformations[x.userID].Reports);

			        Int32 Number = 1;
			        String PlayersInfo = String.Empty;
			        foreach (BasePlayer pList in moderatorSortedList)
			        {
				        PlayersInfo += $"{Number}. {pList.displayName} ({pList.userID}) : {PlayerInformations[pList.userID].Reports} ";
				        PlayersInfo += LanguageEn ? "reports\n" : "жалоб\n";
				        Number++;
			        }

			        if (String.IsNullOrWhiteSpace(PlayersInfo))
				        PlayersInfo = LanguageEn ? "There are no players with complaints" : "Игроков с жалобами нет";
			        
			        if (IsConsole)
				        Puts(LanguageEn ? $"Players with complaints :\n{PlayersInfo}" : $"Игроки с жалобами :\n{PlayersInfo}");
			        else PrintToConsole(player, !lang.GetLanguage(player.UserIDString).Equals("ru") ? $"Players with complaints :\n{PlayersInfo}" : $"Игроки с жалобами :\n{PlayersInfo}");
			        break;
		        }
	        }
        }

		private void DrawUI_ModeratorStitistics_Banner(BasePlayer Moderator, String OffsetMin, String OffsetMax, String TitleBanner, String ArgBanner, String AdditionalText = "", Int32 CountRaiting = -1)
		{
			String Interface = InterfaceBuilder.GetInterface($"{InterfaceBuilder.UI_LAYER}_PROFILE_BANNER_TEMPLATE");
			if (Interface == null) return;

			Interface = Interface.Replace("%OFFSET_MIN%", OffsetMin);
			Interface = Interface.Replace("%OFFSET_MAX%", OffsetMax);
			Interface = Interface.Replace("%TITLE_BANNER%", TitleBanner);
			Interface = Interface.Replace("%ARGS_BANNER%", ArgBanner);

			CuiHelper.AddUi(Moderator, Interface);

			if (!String.IsNullOrWhiteSpace(AdditionalText))
			{
				DrawUI_ModeratorStitistics_Banner_AdditionalText(Moderator, AdditionalText);
				return;
			}

			if (CountRaiting < 0) return;
			
			for (Int32 Raiting = 0; Raiting < CountRaiting; Raiting++)
				DrawUI_ModeratorStitistics_Banner_RaitingImage(Moderator, Raiting);
		}
		
		
				private String GetImage(String fileName, UInt64 skin = 0)
        {
            var imageId = (String)plugins.Find("ImageLibrary").CallHook("GetImage", fileName, skin);
            if (!string.IsNullOrEmpty(imageId))
                return imageId;
            return String.Empty;
        }
		
		private void OnPlayerConnected(BasePlayer player)
		{
			SteamAvatarAdd(player.UserIDString);
			RegisteredPlayer(player);

			CheckStatusPlayer(player);

			if (IsModerator(player))
				CheckStatusModerator(player);
		}

		private void DrawUI_ModeratorStitistics_Banner_AdditionalText(BasePlayer Moderator, String AdditionalText)
		{
			String Interface = InterfaceBuilder.GetInterface($"{InterfaceBuilder.UI_LAYER}_PROFILE_BANNER_TEMPLATE_ADDITIONAL_TEXT");
			if (Interface == null) return;
			
			Interface = Interface.Replace("%ADDITIONAL_TEXT%", AdditionalText);

			CuiHelper.AddUi(Moderator, Interface);
		}
		
		
		
		private void DrawUI_ShowPoopUP(BasePlayer player, String displayName, String userID)
		{
			String Interface = InterfaceBuilder.GetInterface($"{InterfaceBuilder.UI_LAYER}_TEMPLATE_POOPUP");
			if (Interface == null) return;
			
			 Interface = Interface.Replace("%POOPUP_BACKGORUND%",ImageUi.GetImage(config.ImagesSettings.PlayerListBlockSettings.PoopUpBackgorund));
			 Interface = Interface.Replace("%AVATAR%",GetImage(userID));
			 Interface = Interface.Replace("%NICK_NAME%",displayName.Length > 7 ? displayName.Substring(0, 7).ToUpper() + ".." : displayName.ToUpper());
			 Interface = Interface.Replace("%STEAMID%",userID);
			 
			 Interface = Interface.Replace("%TITLE_PLAYER_NICK_NAME%",GetLang("TITLE_PLAYER_NICK_NAME", player.UserIDString));
			 Interface = Interface.Replace("%TITLE_PLAYER_STEAMID%",GetLang("TITLE_PLAYER_STEAMID", player.UserIDString));

			CuiHelper.AddUi(player, Interface);
			
			DrawUI_ShowPoopUP_Reason(player, userID);
		}	

        private void WriteData()
        {
	        Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("IQSystem/IQReportSystem/PlayerInformations", PlayerInformations);
	        Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("IQSystem/IQReportSystem/ModeratorInformations", ModeratorInformations);
        }
		
		private void DrawUI_PoopUp_Moderator_Panel_Info(BasePlayer player, BasePlayer Target, String TitlePanel, List<Configuration.LangText> InfoList, String OffsetMin, String OffsetMax, List<String> AlternativeInfoList = null)
		{
			String Interface = InterfaceBuilder.GetInterface($"{InterfaceBuilder.UI_LAYER}_POOPUP_MODERATOR_INFO_BLOCK");
			if (Interface == null) return;
			
			Interface = Interface.Replace("%TITLE_PANEL%", TitlePanel);
			Interface = Interface.Replace("%OFFSET_MIN%", OffsetMin);
			Interface = Interface.Replace("%OFFSET_MAX%", OffsetMax);

			CuiHelper.AddUi(player, Interface);

			if (AlternativeInfoList != null)
			{
				if (AlternativeInfoList.Count == 0)
				{
					DrawUI_PoopUp_Moderator_InfoText(player, 0, GetLang("TITLE_POOPUP_MODERATION_INFO_BLOCK_EMPTY", player.UserIDString));
					return;
				}
				for (Int32 Y = 0; Y < AlternativeInfoList.Count; Y++)
					DrawUI_PoopUp_Moderator_InfoText(player, Y, AlternativeInfoList[Y]);

				return;
			}
			
			if (InfoList.Count == 0)
			{
				DrawUI_PoopUp_Moderator_InfoText(player, 0, GetLang("TITLE_POOPUP_MODERATION_INFO_BLOCK_EMPTY", player.UserIDString));
				return;
			}
			
			for (Int32 Y = 0; Y < InfoList.Count; Y++)
				DrawUI_PoopUp_Moderator_InfoText(player, Y, InfoList[Y].GetReasonTitle(player.userID));
		}
        
        private List<Fields> DT_StopCheck(UInt64 TargetID, BasePlayer Moderator, Boolean AutoStop = false, Boolean IsConsole = false, Configuration.ReasonReport Verdict = null)
        {
	        String ModeratorName = !IsConsole && Moderator != null ? Moderator.displayName : "Console";
	        String ModeratorID = !IsConsole && Moderator != null ? Moderator.UserIDString : "Console";
	        List<Fields> fields;
	        
	        if (AutoStop)
	        {
		        fields = new List<Fields>
		        {
			        new Fields(LanguageEn ? "Player verification is completed automatically :" : "Проверка игрока завершена автоматически :", "", false),
			        new Fields("", "", false),
			        new Fields(LanguageEn ? "Suspect Information:" : "Информация о подозреваемом :", "", false),
			        new Fields("", "", false),
			        new Fields(LanguageEn ? "Nick" : "Ник", $"{PlayerChecks[TargetID].DisplayName}", true),
			        new Fields("SteamID", $"{TargetID}", true),
			        new Fields("Результат", LanguageEn ? "The player's reports are not reset" : "Репорты игрока не сброшены", false),
		        };
	        }
	        else
	        {
		        fields = new List<Fields>
		        {
			        new Fields(LanguageEn ? "Player check completed :" : "Завершена проверка игрока :", "", false),
			        new Fields("", "", false),
			        new Fields(LanguageEn ? "Moderator Information :" : "Информация о модераторе :", "", false),
			        new Fields("", "", false),
			        new Fields(LanguageEn ? "Nick" : "Ник", $"{ModeratorName}", true),
			        new Fields("Steam64ID", $"[{ModeratorID}](https://steamcommunity.com/profiles/{ModeratorID})", true),
			        new Fields("", "", false),
			        new Fields(LanguageEn ? "Suspect Information:" : "Информация о подозреваемом :", "", false),
			        new Fields("", "", false),
			        new Fields(LanguageEn ? "Nick" : "Ник", $"{PlayerChecks[TargetID].DisplayName}", true),
			        new Fields("SteamID", $"{TargetID}", true),
			        new Fields("Результат", $"{(Verdict == null ? (LanguageEn ? "No violations found" : "Нарушений не выявлено") : LanguageEn ? Verdict.Title.LanguageEN : Verdict.Title.LanguageRU)}", false),
		        };
	        }

	        return fields;
        }

				
				private void StartPluginLoad()
		{
			_ = this;
			
			//AddCommands
			cmd.AddChatCommand(config.CommandForContact, this, nameof(ChatCommandDiscord));
			cmd.AddConsoleCommand(config.CommandForContact, this, nameof(ConsoleCommandDiscord));
			
			//Validate DataFile
			foreach (BasePlayer player in BasePlayer.activePlayerList)
				OnPlayerConnected(player);
			
			//Load your images here
			ImageUi.Initialize();
			ImageUi.DownloadImages();

			//Starting IQFakeActive
			StartSysncFakeActive();
		}
        private void CheckStatusPlayer(BasePlayer player, String ReasonDisconnected = null)
        {
	        if (player == null) return;

	        if (!PlayerChecks.ContainsKey(player.userID)) return;

	        Boolean IsConsole = PlayerChecks[player.userID].ModeratorID == 0;
	        BasePlayer Moderator = BasePlayer.FindByID(PlayerChecks[player.userID].ModeratorID);
	        Configuration.NotifyDiscord.Webhooks.TemplatesNotify TemplateDiscord = config.NotifyDiscordSettings.WebhooksList.NotifyStatusPlayerOrModerator;

	        if (Moderator == null && !IsConsole)
	        {
		        SendChat(GetLang("FUNCIONAL_CHANGE_STATUS_MODERATOR_DISCONNECTED", player.UserIDString), player);
		        
		        Timer WaitModerator = timer.Once(600f, () =>
		        {
			        StopCheckedPlayer(player.userID, null, true);
			        SendChat(GetLang("FUNCIONAL_CHANGE_STATUS_MODERATOR_DISCONNECTED_FULL_LEAVE", player.UserIDString), player);
		        });
		   		 		  						  	   		  	   		  	 				   		 		  				
		        if (!TimerWaitChecked.ContainsKey(player.userID))
			        TimerWaitChecked.Add(player.userID, WaitModerator);
		        else TimerWaitChecked[player.userID] = WaitModerator;

		        return;
	        }

	        if (ReasonDisconnected != null)
	        {
		        if (config.CheckControllerSettings.StopCheckLeavePlayer)
		        {
			        Timer WaitPlayer = timer.Once(900, () => { StopCheckedPlayer(player.userID, Moderator, true); });

			        if (!TimerWaitPlayer.ContainsKey(player.userID))
				        TimerWaitPlayer.Add(player.userID, WaitPlayer);
			        else TimerWaitPlayer[player.userID] = WaitPlayer;
		        }

		        if (IsConsole)
		        {
			        Puts(LanguageEn ? $"The player's connection status has changed from server to : {ReasonDisconnected}" : $"У игрока изменился статус соединения с сервером на : {ReasonDisconnected}");
			        return;
		        }
		        DrawUI_Moderator_Checked_Menu_Status(Moderator, ReasonDisconnected);
		        SendChat(GetLang("FUNCIONAL_CHANGE_STATUS_PLAYER_ALERT_MODERATOR", Moderator.UserIDString, ReasonDisconnected), Moderator);

		        if (!String.IsNullOrWhiteSpace(TemplateDiscord.WebhookNotify))
		        {
			        List<Fields> fields = DT_ChangeStatus(false, player.displayName, player.UserIDString, ReasonDisconnected);
			        SendDiscord(TemplateDiscord.WebhookNotify, fields, GetAuthorDiscord(TemplateDiscord), TemplateDiscord.Color);
		        }
		        
		        SendVK(VKT_ChangeStatus(false, player.displayName, player.UserIDString, ReasonDisconnected));
		        
		        StopDamageRemove(player.userID);
		        return;
	        }
	        
	        if (!player.IsConnected) return;

	        player.Invoke(() =>
	        {
		        SendChat(GetLang("FUNCIONAL_CHANGE_STATUS_PLAYER_ONLINE_ALERT_PLAYER", player.UserIDString), player);
		        DrawUI_Player_Alert(player);
		        
		        if(config.CheckControllerSettings.UseDemo)
			        player.StartDemoRecording();
		        
		        if (IsConsole)
		        {
			        Puts(LanguageEn ? "The player has connected to the server. Check continued" : "Игрок подключился к серверу. Проверка продолжена");
			        return;
		        }
		        if (Moderator == null) return;
		        DrawUI_Moderator_Checked_Menu_Status(Moderator, GetLang("TITLE_PROFILE_MODERATOR_CHECKED_MENU_TITLE_STATUS_DEFAULT", Moderator.UserIDString));
		        SendChat(GetLang("FUNCIONAL_CHANGE_STATUS_PLAYER_ONLINE_ALERT_MODERATOR", Moderator.UserIDString), Moderator);
		        
		        StopDamageAdd(player);
	        }, 3f);
	        
	        if (!String.IsNullOrWhiteSpace(TemplateDiscord.WebhookNotify))
	        {
		        List<Fields> fields = DT_ChangeStatus(false, player.displayName, player.UserIDString, "Online");
		        SendDiscord(TemplateDiscord.WebhookNotify, fields, GetAuthorDiscord(TemplateDiscord), TemplateDiscord.Color);
	        }
	        
	        SendVK(VKT_ChangeStatus(false, player.displayName, player.UserIDString, "Online"));

	        
	        if (!TimerWaitPlayer.ContainsKey(player.userID)) return;
	        if (TimerWaitPlayer[player.userID].Destroyed) return;
	        
	        TimerWaitPlayer[player.userID].Destroy();
	        TimerWaitPlayer.Remove(player.userID);
        }

		
		
		
		private static Configuration config = new Configuration();

		private String CorrectedClanName(BasePlayer player)
		{
			String ClanTag = GetClanTag(player.UserIDString);
			String pattern = @"\[.*?\]";
			
			String CorrectedResult = ClanTag == null || String.IsNullOrWhiteSpace(ClanTag)
				? player.displayName
				: Regex.Replace(player.displayName, pattern, String.Empty);

			if (String.IsNullOrWhiteSpace(CorrectedResult) || CorrectedResult == null)
				CorrectedResult = player.displayName;
			
			return CorrectedResult;
		}
		
		private void DrawUI_TemplatePlayer_Moderator_IsSteam(BasePlayer player, String UserID)
		{
			String Interface = InterfaceBuilder.GetInterface($"{InterfaceBuilder.UI_LAYER}_TEMPLATE_PLAYER_MODERATOR_ISSTEAM");
			if (Interface == null) return;

			Interface = Interface.Replace("%STATUS_PLAYER%", IsSteam(UserID) ? ImageUi.GetImage(config.ImagesSettings.ModeratorMenuCheckedSettings.SteamIcoPlayer) : ImageUi.GetImage(config.ImagesSettings.ModeratorMenuCheckedSettings.PirateIcoPlayer));

			CuiHelper.AddUi(player, Interface);
		}

        private void AddCooldown(UInt64 SenderID, UInt64 TargetID)
        {
	        if (config.ReportSendControllerSettings.CooldownReport == 0 && !config.ReportSendControllerSettings.NoRepeatReport) return;
	        if (!PlayerRepositories.ContainsKey(SenderID))
	        {
		        PlayerRepositories.Add(SenderID, new PlayerRepository
		        {
			        ReportedList = new Dictionary<UInt64, Double>(),
			        Cooldown = 0
		        });
	        }

	        PlayerRepositories[SenderID].AddCooldown(TargetID);
        }
		
		private void DrawUI_Player_Alert(BasePlayer player)
		{
			String Interface = InterfaceBuilder.GetInterface($"{InterfaceBuilder.UI_LAYER}_PLAYER_ALERT");
			if (Interface == null) return;
	
			Interface = Interface.Replace("%TITLE_TEXT%", GetLang("TITLE_PLAYER_ALERT_INFORMATION_TITLE", player.UserIDString));
			Interface = Interface.Replace("%DESCRIPTION_TEXT%", GetLang("TITLE_PLAYER_ALERT_INFORMATION_DESCRIPTION", player.UserIDString));
			
			CuiHelper.DestroyUi(player, InterfaceBuilder.UI_REPORT_PLAYER_ALERT);
			CuiHelper.AddUi(player, Interface);
			
			if (!config.CheckControllerSettings.UseSoundAlert) return;
			NextTick(() => { SoundPlay(player); });
		}

        
        
        private void SendRaitingModerator(UInt64 ModeratorID, Int32 IndexAchive, Int32 StarAmount)
        {
	        if (!ModeratorInformations.ContainsKey(ModeratorID)) return;

	        ModeratorInformation ModeratorInfo = ModeratorInformations[ModeratorID];
	        List<Int32> ListScore = IndexAchive == 1 ? ModeratorInfo.OneScore :
		        IndexAchive == 2 ? ModeratorInfo.TwoScore :
		        IndexAchive == 3 ? ModeratorInfo.ThreeScore : null;

	        ListScore?.Add(StarAmount);
	        
	        Interface.Call("OnSendedFeedbackChecked", ModeratorID, IndexAchive, StarAmount);
        }

        private void ReadData()
        {
	        PlayerInformations = Oxide.Core.Interface.Oxide.DataFileSystem.ReadObject<Dictionary<UInt64, PlayerInformation>>("IQSystem/IQReportSystem/PlayerInformations");
	        ModeratorInformations = Oxide.Core.Interface.Oxide.DataFileSystem.ReadObject<Dictionary<UInt64, ModeratorInformation>>("IQSystem/IQReportSystem/ModeratorInformations");
        }
        private Dictionary<UInt64, Timer> TimerWaitPlayer = new Dictionary<UInt64, Timer>();
		
		private List<String> GetServersBansRCC(UInt64 TargetID)
		{
			if (String.IsNullOrWhiteSpace(config.ReferenceSettings.RCCSettings.RCCKey)) return null;
			return !RCC_LocalRepository.ContainsKey(TargetID) ? new List<String>() : RCC_LocalRepository[TargetID].LastBansServers;
		}

		private void OnPlayerDisconnected(BasePlayer player, String reason)
		{
			CheckStatusPlayer(player, reason);
			
			if (IsModerator(player))
				CheckStatusModerator(player, reason);
		} 
        private List<Fields> DT_PlayerSendContact(BasePlayer Sender, String Contact)
        {
	        List<Fields> fields = new List<Fields>
	        {
		        new Fields(LanguageEn ? "Information about the sender :" : "Информация об отправителе :", "", false),
		        new Fields("", "", false),
		        new Fields(LanguageEn ? "Nick" : "Ник", $"{Sender.displayName}", true),
		        new Fields("Steam64ID", $"[{Sender.userID}](https://steamcommunity.com/profiles/{Sender.userID})", true),
		        new Fields(LanguageEn ? "Contacts for communication :" : "Контакты для связи :", Contact, false),
	        };

	        return fields;
        }

        
        
        
        public Dictionary<BasePlayer, Coroutine> RoutineSounds = new Dictionary<BasePlayer, Coroutine>();

		
		
		
		private Dictionary<UInt64, LocalRepositoryOzProtect> OzProtect_LocalRepository = new Dictionary<UInt64, LocalRepositoryOzProtect>();
		protected override void SaveConfig() => Config.WriteObject(config);
		
				public class OzResponse
		{
			public int unixtime { get; set; }
			public string reason { get; set; }
			public string proofid { get; set; }
			public bool pirate { get; set; }
			public bool active { get; set; }
			public bool reliable { get; set; }
			public bool unnecessary { get; set; }
			public OzServer server { get; set; }
			public string admin { get; set; }
			public int game { get; set; }
			public string date { get; set; }
			public int bantime { get; set; }
		}
		
		private void DrawUI_Moderator_Checked_Menu_Discord(BasePlayer moderator, String Discord)
		{
			String Interface = InterfaceBuilder.GetInterface($"{InterfaceBuilder.UI_LAYER}_DISCORD_STATUS_CHECKED_MODERATOR");
			if (Interface == null) return;
		   		 		  						  	   		  	   		  	 				   		 		  				
			Interface = Interface.Replace("%TITLE_PROFILE_MODERATOR_CHECKED_MENU_TITLE_DISCORD%", GetLang("TITLE_PROFILE_MODERATOR_CHECKED_MENU_TITLE_DISCORD", moderator.UserIDString, Discord));

			CuiHelper.DestroyUi(moderator, "InfoDiscord");
			CuiHelper.AddUi(moderator, Interface);
		}

		private String GetClanTag(String userID)
		{
			if (Clans)
				return (String)Clans.Call("GetClanOf", userID);
			else return null;
		}
		
		private void DrawUI_Raiting_Menu_Stars(BasePlayer player, Int32 SelectedAmount, UInt64 ModeratorID)
		{
			for (Int32 X = 0; X < 5; X++)
			{
				String Interface =
					InterfaceBuilder.GetInterface($"{InterfaceBuilder.UI_LAYER}_RAITING_MENU_PLAYER_STARS");
				if (Interface == null) return;

				Interface = Interface.Replace("%OFFSET_MIN%", $"{-88.24 + (X * 15)} -22");
				Interface = Interface.Replace("%OFFSET_MAX%", $"{-77.58 + (X * 15)} -12");
				Interface = Interface.Replace("%COLOR_STARS%", SelectedAmount >= X ? "1 1 1 1" : "1 1 1 0.5");
				Interface = Interface.Replace("%COMMAND_STARS%", SelectedAmount >= 0 ? "" : $"report.panel select.raiting.star {X} {ModeratorID}");

				CuiHelper.AddUi(player, Interface);
			}
		}

                
              
        public Boolean IsValidStartChecked(BasePlayer Target, BasePlayer Moderator, Boolean IsConsole = false)
        {
	        if (PlayerChecks.ContainsKey(Target.userID))
	        {
		        if (IsConsole)
			        Puts(LanguageEn ? "This player has already been called for checked" : "Данного игрока уже вызвали на проверку");
		        else if(Moderator != null)
			        SendChat(GetLang("NOTIFY_MODERATOR_ITS_PLAYER_CHECKED", Moderator.UserIDString), Moderator);
		        return false;
	        }
	        
	        if (IsRaidBlock(Target)) 
	        {
		        if (IsConsole)
			        Puts(LanguageEn ? "The check was canceled automatically with complaints saved! Reason : the player has an active raid-block" : "Проверка отменена автоматически с сохранением жалоб! Причина : у игрока активный рейд-блок");
		        else if(Moderator != null)
			        SendChat(GetLang("NOTIFY_MODERATOR_RAIDBLOCK_PLAYER", Moderator.UserIDString), Moderator);
		        return false;
	        }

	        if (IsCombatBlock(Target))
	        {
		        if (IsConsole)
			        Puts(LanguageEn ? "The check was canceled automatically with complaints saved! Reason : the player has an active combat-block" : "Проверка отменена автоматически с сохранением жалоб! Причина : у игрока активный комбат-блок");
		        else if(Moderator != null)
			        SendChat(GetLang("NOTIFY_MODERATOR_COMBATBLOCK_PLAYER", Moderator.UserIDString), Moderator);
		        return false;
	        }

	        if (IsDuel(Target.userID))
	        {
		        if (IsConsole)
			        Puts(LanguageEn ? "The check is canceled automatically with complaints saved! Reason : the player is in a duel" : "Проверка отменена автоматически с сохранением жалоб! Причина : игрок находится на дуэли");
		        else if(Moderator != null)
			        SendChat(GetLang("NOTIFY_MODERATOR_DUEL_PLAYER", Moderator.UserIDString), Moderator);
		        return false;
	        }

	        if (Moderator != null && !IsConsole)
	        {
		        if (IsFriendStartChecked(Moderator.userID, Target.userID))
		        {
			        SendChat(GetLang("NOTIFY_MODERATOR_FRIEND_PLAYER", Moderator.UserIDString), Moderator);
			        return false;
		        }
		        if (IsClansStartChecked(Moderator.UserIDString, Target.UserIDString))
		        {
			        SendChat(GetLang("NOTIFY_MODERATOR_FRIEND_PLAYER", Moderator.UserIDString), Moderator);
			        return false;
		        }
	        }
	        
	        return true;
        }

		protected override void LoadDefaultConfig() => config = Configuration.GetNewConfiguration();

				private void SendChat(String Message, BasePlayer player, ConVar.Chat.ChatChannel channel = ConVar.Chat.ChatChannel.Global)
		{
		    if (IQChat)
			    if (config.ReferenceSettings.IQChatSetting.UIAlertUse)
				    IQChat?.Call("API_ALERT_PLAYER_UI", player, Message);
			    else IQChat?.Call("API_ALERT_PLAYER", player, Message, config.ReferenceSettings.IQChatSetting.CustomPrefix, config.ReferenceSettings.IQChatSetting.CustomAvatar);
		    else player.SendConsoleCommand("chat.add", channel, 0, Message); 
		}
        private class InterfaceBuilder
        {
            
            public static InterfaceBuilder Instance;
            
            public const String UI_LAYER = "UI_IQREPORT_INTERFACE";
            
            public const String UI_REPORT_PANEL = "UI_REPORT_PANEL";
            public const String UI_REPORT_LEFT_PANEL = "UI_REPORT_LEFT_PANEL";
	        public const String UI_REPORT_PLAYER_PANEL = "UI_REPORT_PLAYER_PANEL";
	        public const String UI_REPORT_POOPUP_PLAYER = "UI_REPORT_POOPUP_PLAYER";
	        public const String UI_REPORT_POOPUP_MODERATOR = "UI_REPORT_POOPUP_MODERATOR";
	        public const String UI_REPORT_MODERATOR_STATISTICS = "UI_REPORT_MODERATOR_STATISTICS";
	        public const String UI_REPORT_MODERATOR_MENU_CHECKED = "UI_REPORT_MODERATOR_MENU_CHECKED";
	        public const String UI_REPORT_RAITING_PLAYER_PANEL = "UI_REPORT_RAITING_PLAYER_PANEL";
	        public const String UI_REPORT_PLAYER_ALERT = "UI_REPORT_PLAYER_ALERT";

	        public Dictionary<String, String> Interfaces;

            
            
            public InterfaceBuilder()
            {
                Instance = this;
                Interfaces = new Dictionary<String, String>();

                Building_Bacgkround();
                
                Building_Panel_Players();
                Building_PageController();
                Building_PlayerTemplate();
                Building_PoopUp_Player();
                Building_PoopUp_Reason();
                
                Building_PlayerTemplate_Moderator();
                Building_Left_Menu();
                Building_Button_Template();
                Building_PoopUP_Moderator();
                Building_PoopUP_Moderator_InfoBlock();
                Building_Text_Template_Moderator_Block_Info();
                
                Building_HeaderPanel_Search();

                Building_Profile_Moderator_Stats();
                Building_Profile_Template_Banner();
                Building_Profile_Template_Banner_AdditionalImg();
                Building_Profile_Template_Banner_AdditionalText();

                Building_Moderator_Menu();
				Building_ModeratorMenuChecked_InfoDiscord();
				Building_ModeratorMenuChecked_InfoOnline();
				Building_ModeratorMenu_Verdict_Button();
                
                Building_Raiting_Menu();
                Building_Raiting_Select_Button();
                
                Building_DropList_Reasons();
                
                Building_Player_Alert();

                Building_PlayerTemplate_Moderator_IsSteam();
            }

            public static void AddInterface(String name, String json)
            {
                if (Instance.Interfaces.ContainsKey(name))
                {
                    _.PrintError($"Error! Tried to add existing cui elements! -> {name}");
                    return;
                }
		   		 		  						  	   		  	   		  	 				   		 		  				
                Instance.Interfaces.Add(name, json);
            }
		   		 		  						  	   		  	   		  	 				   		 		  				
            public static String GetInterface(String name)
            {
                String json = String.Empty;
                if (Instance.Interfaces.TryGetValue(name, out json) == false)
                {
                    _.PrintWarning($"Warning! UI elements not found by name! -> {name}");
                }

                return json;
            }

            public static void DestroyAll()
            {
                for (Int32 i = 0; i < BasePlayer.activePlayerList.Count; i++)
                {
                    BasePlayer player = BasePlayer.activePlayerList[i];
                    CuiHelper.DestroyUi(player, UI_REPORT_PANEL);
                    CuiHelper.DestroyUi(player, UI_REPORT_MODERATOR_MENU_CHECKED);
                    CuiHelper.DestroyUi(player, UI_REPORT_RAITING_PLAYER_PANEL);
                    CuiHelper.DestroyUi(player, UI_REPORT_PLAYER_ALERT);
                }
            }
		   		 		  						  	   		  	   		  	 				   		 		  				
            
            
            private void Building_Bacgkround()
            {
	            CuiElementContainer container = new CuiElementContainer();

	            container.Add(new CuiPanel
	            {
		            CursorEnabled = true,
		            Image = { Color = "0 0 0 0" },
		            RectTransform ={ AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-459.83 -316.66", OffsetMax = "461.49 316.67" }
	            },"Overlay",UI_REPORT_PANEL);
	            
	            container.Add(new CuiButton
	            {
		            RectTransform = { AnchorMin = "-100 -100", AnchorMax = "100 100" },
		            Button = { Close = UI_REPORT_PANEL, Color = "0 0 0 0" },
		            Text = { Text = "" }
	            }, UI_REPORT_PANEL);
	            
	            container.Add(new CuiElement
	            {
		            Name = "PNG_BACKGORUND",
		            Parent = UI_REPORT_PANEL,
		            Components = {
			            new CuiRawImageComponent { Color = config.ColorsSettings.MainColor, Png = ImageUi.GetImage(config.ImagesSettings.Background) },
			            new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
		            }
	            });
	            
	            container.Add(new CuiElement
	            {
		            Name = "TitleUi",
		            Parent = UI_REPORT_PANEL,
		            Components = {
			            new CuiTextComponent { Text = "%TITLE_NAME_REPORT_SYSTEM%", Font = "robotocondensed-regular.ttf", FontSize = 18, Align = TextAnchor.MiddleCenter, Color = config.ColorsSettings.MainColorText },
			            new CuiRectTransformComponent { AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = "38.72 242.11", OffsetMax = "180.96 276.15" }
		            }
	            });
	            
	            
	            container.Add(new CuiPanel
	            {
		            CursorEnabled = false,
		            Image = { Color = "0 0 0 0" },
		            RectTransform ={ AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = "-265.27 -93.21", OffsetMax = "224.90 -28.37" }
	            },UI_REPORT_PANEL,"HeaderUI");

	            container.Add(new CuiElement
	            {
		            Name = "HeaderActionTitle",
		            Parent = "HeaderUI",
		            Components = {
			            new CuiTextComponent { Text = "%TITLE_PLAYER_HEADER_TITLE_SEND_REPORT%", Font = "robotocondensed-regular.ttf", FontSize = 18, Align = TextAnchor.UpperLeft, Color = config.ColorsSettings.MainColorText },
			            new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-219.38 -21.38", OffsetMax = "-33.95 4.11" }
		            }
	            });

	            container.Add(new CuiElement
	            {
		            Name = "HeaderActionDescription",
		            Parent = "HeaderUI",
		            Components = {
			            new CuiTextComponent { Text = "%TITLE_PLAYER_HEADER_TITLE_DESC_SEND_REPORT%", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.UpperLeft, Color = config.ColorsSettings.AdditionalColorText },
			            new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-219.38 0", OffsetMax = "-78.11 19.54" }
		            }
	            });

	            container.Add(new CuiElement
	            {
		            Name = "HeaderSearchIcon",
		            Parent = "HeaderUI",
		            Components = {
			            new CuiRawImageComponent { Color = config.ColorsSettings.MainColorText, Png = ImageUi.GetImage(config.ImagesSettings.Search)},
			            new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "136.33 -6.55", OffsetMax = "147 4.11" }
		            }
	            });

	            
	            
	            container.Add(new CuiPanel
	            {
		            CursorEnabled = false,
		            Image = { Color = "0 0 0 0" },
		            RectTransform ={ AnchorMin = "1 0.5", AnchorMax = "1 0.5", OffsetMin = "-235.75 -288.17", OffsetMax = "-28.33 288.29" }
	            },UI_REPORT_PANEL,"ProfilePanel");

	            container.Add(new CuiElement
	            {
		            Name = "AvatarUser",
		            Parent = "ProfilePanel",
		            Components = {
			            new CuiRawImageComponent { Color = "1 1 1 1", Png = "%AVATAR_PLAYER%"},
			            new CuiRectTransformComponent { AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = "-66.32 -51.29", OffsetMax = "-23.66 -8.63" }
		            }
	            });

	            container.Add(new CuiElement
	            {
		            Name = "CircleBlock",
		            Parent = "AvatarUser",
		            Components = {
			            new CuiRawImageComponent { Color = config.ColorsSettings.MainColor, Png = ImageUi.GetImage(config.ImagesSettings.AvatarBlur)},
			            new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1"}
		            }
	            });

	            container.Add(new CuiElement
	            {
		            Name = "Nick",
		            Parent = "ProfilePanel",
		            Components = {
			            new CuiTextComponent { Text = "%NICK_PROFILE%", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.UpperLeft, Color = config.ColorsSettings.MainColorText },
			            new CuiRectTransformComponent { AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = "-19.05 -34.22", OffsetMax = "82.47 -15.09" }
		            }
	            });
		   		 		  						  	   		  	   		  	 				   		 		  				
	            container.Add(new CuiElement
	            {
		            Name = "CheckedCount",
		            Parent = "ProfilePanel",
		            Components = {
			            new CuiTextComponent { Text = "%TITLE_PROFILE_INFO_CHECKED%", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.UpperLeft, Color = config.ColorsSettings.AdditionalColorText },
			            new CuiRectTransformComponent { AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = "-19.05 -45.29", OffsetMax = "75.05 -29.37" }
		            }
	            });


	            	            
	            container.Add(new CuiButton
	            {
		            Button = { Color = "0 0 0 0", Close = UI_REPORT_PANEL},
		            Text = { Text = "%TITLE_CLOSE_BUTTON_REPORT%", Font = "robotocondensed-regular.ttf", FontSize = 17, Align = TextAnchor.MiddleCenter, Color = config.ColorsSettings.MainColorText },
		            RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "405.26 265.06", OffsetMax = "426.6 286.4" }
	            },UI_REPORT_PANEL,"CloseReportPanel");

	            AddInterface($"{UI_LAYER}_BACKGORUND", container.ToJson());
            }
            
            private void Building_HeaderPanel_Search()
            {
	            CuiElementContainer container = new CuiElementContainer();
	            
	            String NickName = "";
	            container.Add(new CuiPanel
	            {
		            RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "152.25 -9.98", OffsetMax = "245.09 7.53" },
		            Image = { Color = "0 0 0 0" }
	            }, "HeaderUI", "InputPanelSearch" + ".Input");

	            container.Add(new CuiElement
	            {
		            Parent = "InputPanelSearch" + ".Input",
		            Name = "InputPanelSearch" + ".Input.Current",
		            Components =
		            {
			            new CuiInputFieldComponent { Text = "%TITLE_PLAYER_HEADER_TITLE_SEARCH_PLAYER%", FontSize = 14, Command = $"report.panel search.player %ISMODERATOR% {NickName}", Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleLeft, Color = config.ColorsSettings.AdditionalColorText, CharsLimit = 13, NeedsKeyboard = true},
			            new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
		            }
	            });
	            
	            AddInterface($"{UI_LAYER}_SEARCH_HEADER", container.ToJson());
            }

            
            private void Building_Profile_Moderator_Stats()
            {
	            CuiElementContainer container = new CuiElementContainer();
	            
	            container.Add(new CuiPanel
	            {
		            CursorEnabled = false,
		            Image = { Color = "0 0 0 0" },
		            RectTransform ={ AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-103.71 -288.23", OffsetMax = "103.71 224.66" }
	            },"ProfilePanel",UI_REPORT_MODERATOR_STATISTICS);
	            
	            container.Add(new CuiElement
	            {
		            Name = "TitleStatistics",
		            Parent = UI_REPORT_MODERATOR_STATISTICS,
		            Components = {
			            new CuiTextComponent { Text = "%TITLE_PROFILE_MODERATOR_STATISTICS_TITLE%", Font = "robotocondensed-regular.ttf", FontSize = 18, Align = TextAnchor.UpperLeft, Color = config.ColorsSettings.MainColorText },
			            new CuiRectTransformComponent { AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = "-93.21 -40.28", OffsetMax = "91.72 -13.05" }
		            }
	            });
	            
	            container.Add(new CuiElement
	            {
		            Name = "TitleRaiting",
		            Parent = UI_REPORT_MODERATOR_STATISTICS,
		            Components = {
			            new CuiTextComponent { Text = "%TITLE_PROFILE_MODERATOR_STATISTICS_TITLE_QUALITY_ASSESSMENT%", Font = "robotocondensed-regular.ttf", FontSize = 18, Align = TextAnchor.UpperLeft, Color = config.ColorsSettings.MainColorText },
			            new CuiRectTransformComponent { AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = "-93.21 -210.09", OffsetMax = "91.72 -182.86" }
		            }
	            });
	            
	            AddInterface($"{UI_LAYER}_PROFILE_MODERATION_INFO_PANEL", container.ToJson());
            }
		   		 		  						  	   		  	   		  	 				   		 		  				
            private void Building_Profile_Template_Banner()
            {
	            CuiElementContainer container = new CuiElementContainer();
	            
	            container.Add(new CuiElement
	            {
		            Name = "CheckedPanel",
		            Parent = UI_REPORT_MODERATOR_STATISTICS,
		            Components = {
			            new CuiRawImageComponent { Color = config.ColorsSettings.AdditionalColorElements, Png = ImageUi.GetImage(config.ImagesSettings.StatisticsBlockSettings.BlockStatsModeration) },
			            new CuiRectTransformComponent { AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = "%OFFSET_MIN%", OffsetMax = "%OFFSET_MAX%"}
		            }
	            });

	            container.Add(new CuiElement
	            {
		            Name = "TitleChecked",
		            Parent = "CheckedPanel",
		            Components = {
			            new CuiTextComponent { Text = "%TITLE_BANNER%", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.UpperLeft, Color = config.ColorsSettings.MainColorText },
			            new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-61.85 -4.69", OffsetMax = "28.22 13.37" }
		            }
	            });

	            container.Add(new CuiElement
	            {
		            Name = "CountPanelChecked",
		            Parent = "CheckedPanel",
		            Components = {
			            new CuiRawImageComponent { Color = config.ColorsSettings.AdditionalColorElementsTwo, Png = ImageUi.GetImage(config.ImagesSettings.StatisticsBlockSettings.BlockStatsRaitingModeration) },
			            new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "33.73 -9.33", OffsetMax = "77.06 9.33"}
		            }
	            });

	            container.Add(new CuiElement
	            {
		            Name = "CountInfoChecked",
		            Parent = "CountPanelChecked",
		            Components = {
			            new CuiTextComponent { Text = "%ARGS_BANNER%", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = config.ColorsSettings.MainColorText },
			            new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-21.66 -9.33", OffsetMax = "21.66 9.33" }
		            }
	            });
	            
	            AddInterface($"{UI_LAYER}_PROFILE_BANNER_TEMPLATE", container.ToJson());
            }
            
            private void Building_Profile_Template_Banner_AdditionalText()
            {
	            CuiElementContainer container = new CuiElementContainer();

	            container.Add(new CuiElement
	            {
		            Name = "TitleCheckedAllTime",
		            Parent = "CheckedPanel",
		            Components = {
			            new CuiTextComponent { Text = "%ADDITIONAL_TEXT%", Font = "robotocondensed-regular.ttf", FontSize = 12, Align = TextAnchor.LowerLeft, Color = config.ColorsSettings.AdditionalColorText },
			            new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-61.85 -13.21", OffsetMax = "28.22 2.40" }
		            }
	            });

	            AddInterface($"{UI_LAYER}_PROFILE_BANNER_TEMPLATE_ADDITIONAL_TEXT", container.ToJson());
            }
            
            private void Building_Profile_Template_Banner_AdditionalImg()
            {
	            CuiElementContainer container = new CuiElementContainer();

	            container.Add(new CuiElement
	            {
		            Name = "RateImageOne",
		            Parent = "CheckedPanel",
		            Components = {
			            new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage(config.ImagesSettings.StatisticsBlockSettings.RaitingImage) },
			            new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "%OFFSET_MIN%", OffsetMax = "%OFFSET_MAX%" }
		            }
	            });

	            AddInterface($"{UI_LAYER}_PROFILE_BANNER_TEMPLATE_ADDITIONAL_IMG", container.ToJson());
            }

                        
            
            
            private void Building_Panel_Players()
            {
	            CuiElementContainer container = new CuiElementContainer();

	            container.Add(new CuiPanel
	            {
		            CursorEnabled = false,
		            Image = { Color = "0.8 0 0 0" },
		            RectTransform =
		            {
			            AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-265.27 -288.17",
			            OffsetMax = "224.90 223.45"
		            }
	            }, UI_REPORT_PANEL, UI_REPORT_PLAYER_PANEL);

	            container.Add(new CuiElement
	            {
		            Name = "ActionSearch",
		            Parent = UI_REPORT_PLAYER_PANEL,
		            Components =
		            {
			            new CuiTextComponent
			            {
				            Text = "%TITLE_PLAYER_LIST%", Font = "robotocondensed-regular.ttf", FontSize = 18,
				            Align = TextAnchor.UpperLeft, Color = config.ColorsSettings.MainColorText
			            },
			            new CuiRectTransformComponent
			            {
				            AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-219.21 212.58", OffsetMax = "56.36 244.75"
			            }
		            }
	            });

	            container.Add(new CuiElement
	            {
		            Name = "ActionPageNext",
		            Parent = UI_REPORT_PLAYER_PANEL,
		            Components =
		            {
			            new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage(config.ImagesSettings.PageUp)},
			            new CuiRectTransformComponent
			            {
				            AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "170.99 231.02",
				            OffsetMax = "177.66 234.35"
			            }
		            }
	            });

	            container.Add(new CuiElement
	            {
		            Name = "ActionPageBack",
		            Parent = UI_REPORT_PLAYER_PANEL,
		            Components =
		            {
			            new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage(config.ImagesSettings.PageDown) },
			            new CuiRectTransformComponent
			            {
				            AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "210.38 231.02",
				            OffsetMax = "217.04 234.35"
			            }
		            }
	            });
	            
	            AddInterface($"{UI_LAYER}_PANEL_PLAYERS", container.ToJson());
            }

            private void Building_PageController()
            {
	            CuiElementContainer container = new CuiElementContainer();

	            container.Add(new CuiElement
	            {
		            Name = "ActionPageCount",
		            Parent = UI_REPORT_PLAYER_PANEL,
		            Components =
		            {
			            new CuiTextComponent
			            {
				            Text = "%AMOUNT_PAGE%", Font = "robotocondensed-regular.ttf", FontSize = 14,
				            Align = TextAnchor.MiddleCenter, Color = config.ColorsSettings.MainColorText
			            },
			            new CuiRectTransformComponent
			            {
				            AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "177.65 224.77", OffsetMax = "210.38 242.86"
			            }
		            }
	            });
	            
	            container.Add(new CuiButton
	            { 
		            Button = { Color = "0 0 0 0", Command = "%COMMAND_PAGE_NEXT%" },
		            Text = { Text = "", Font = "robotocondensed-regular.ttf", FontSize = 40, Align = TextAnchor.MiddleCenter, Color = "0 0 0 0" },
		            RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1"}
	            }, "ActionPageNext", "ButtonNextPage");
	            
	            container.Add(new CuiButton
	            { 
		            Button = { Color = "0 0 0 0", Command = "%COMMAND_PAGE_BACK%" },
		            Text = { Text = "", Font = "robotocondensed-regular.ttf", FontSize = 40, Align = TextAnchor.MiddleCenter, Color = "0 0 0 0" },
		            RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1"}
	            }, "ActionPageBack", "ButtonBackPage");

	            AddInterface($"{UI_LAYER}_PANEL_PLAYERS_PAGE_CONTROLLER", container.ToJson());
            }
            
            
            
             private void Building_PlayerTemplate()
            {
	            CuiElementContainer container = new CuiElementContainer();
	
				container.Add(new CuiPanel 
				{
		            CursorEnabled = false,
		            Image = { Color = "0 0 0 0" },
		            RectTransform =
		            {
			            AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "%OFFSET_MIN%", OffsetMax = "%OFFSET_MAX%"
		            }
	            }, UI_REPORT_PLAYER_PANEL, "PlayerPanel");

	            container.Add(new CuiElement
	            {
		            Name = "AvatarPlayer",
		            Parent = "PlayerPanel",
		            Components =
		            {
			            new CuiRawImageComponent { Color = "1 1 1 1", Png = "%STEAMID%"},
			            new CuiRectTransformComponent
			            {
				            AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-64.62 -18", OffsetMax = "-28.62 18"
			            }
		            }
	            });
	            
	            container.Add(new CuiElement
	            {
		            Name = "BlurAvatar",
		            Parent = "AvatarPlayer",
		            Components =
		            {
			            new CuiRawImageComponent { Color = config.ColorsSettings.MainColor, Png = ImageUi.GetImage(config.ImagesSettings.AvatarBlur)},
			            new CuiRectTransformComponent
			            {
				            AnchorMin = "0 0", AnchorMax = "1 1"
			            }
		            }
	            });

	            container.Add(new CuiElement
	            {
		            Name = "NickName",
		            Parent = "PlayerPanel",
		            Components =
		            {
			            new CuiTextComponent
			            {
				            Text = "%NICK%", Font = "robotocondensed-regular.ttf", FontSize = 16,
				            Align = TextAnchor.MiddleLeft, Color = config.ColorsSettings.MainColorText
			            },
			            new CuiRectTransformComponent
			            {
				            AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-25.32 -2.93", OffsetMax = "64.62 18"
			            }
		            }
	            });
	            
	            container.Add(new CuiElement
	            {
		            Name = "NickNameTitle",
		            Parent = "PlayerPanel",
		            Components =
		            {
			            new CuiTextComponent
			            {
				            Text = "%TITLE_PLAYER_NICK_NAME%", Font = "robotocondensed-regular.ttf", FontSize = 16,
				            Align = TextAnchor.LowerLeft, Color = config.ColorsSettings.AdditionalColorText
			            },
			            new CuiRectTransformComponent
			            {
				            AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-25.32 -18", OffsetMax = "64.62 3.41"
				            
			            }
		            }
	            });
	            
	            container.Add(new CuiButton
	            { 
		            Button = { Color = "0 0 0 0", Command = "%COMMAND%" },
		            Text = { Text = "", Font = "robotocondensed-regular.ttf", FontSize = 40, Align = TextAnchor.MiddleCenter, Color = "0 0 0 0" },
		            RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1"}
	            }, "PlayerPanel", "SelectUserForReport");
	            
	            AddInterface($"{UI_LAYER}_TEMPLATE_PLAYER", container.ToJson());
            }
		   		 		  						  	   		  	   		  	 				   		 		  				
            
            
            private void Building_PoopUp_Player()
            {
	            CuiElementContainer container = new CuiElementContainer();

	            container.Add(new CuiPanel
	            {
		            Image = { Color = "0 0 0 0.3", Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" },
		            RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-432.18 -288.17", OffsetMax = "432.34 288.29" }
	            }, UI_REPORT_PANEL, "BLURED_POOP_UP");
	            
	            container.Add(new CuiButton
	            {
		            RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
		            Button = { Close = "UI_REPORT_PANEL_CLOSE", Command = "report.panel close.poopup",  Color = "0 0 0 0" },
		            Text = { Text = "", Font = "robotocondensed-regular.ttf", FontSize = 13, Align = TextAnchor.MiddleCenter, Color = "0 0 0 0"}
	            },  "BLURED_POOP_UP", "UI_REPORT_PANEL_CLOSE");
	            
	            container.Add(new CuiElement
	            {
		            Name = UI_REPORT_POOPUP_PLAYER,
		            Parent = UI_REPORT_PANEL,
		            Components =
		            {
			            new CuiRawImageComponent { Color = config.ColorsSettings.MainColor, Png = "%POOPUP_BACKGORUND%" },
			            new CuiRectTransformComponent
			            {
				            AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-188.59 -199.06", OffsetMax = "189.40 200.26"
			            }
		            }
	            });

	            container.Add(new CuiElement
	            {
		            Name = "PoopUpAvatar",
		            Parent = UI_REPORT_POOPUP_PLAYER,
		            Components =
		            {
			            new CuiRawImageComponent { Color = "1 1 1 1", Png = "%AVATAR%"},
			            new CuiRectTransformComponent
			            {
				            AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-152.46 113.8",
				            OffsetMax = "-109.8 156.46"
			            }
		            }
	            });

	            container.Add(new CuiElement
	            {
		            Name = "PoopUpCircle",
		            Parent = "PoopUpAvatar",
		            Components =
		            {
			            new CuiRawImageComponent { Color = config.ColorsSettings.MainColor, Png = ImageUi.GetImage(config.ImagesSettings.AvatarBlur) },
			            new CuiRectTransformComponent
			            {
				            AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-21.33 -21.33",
				            OffsetMax = "21.33 21.33"
			            }
		            }
	            });

	            container.Add(new CuiElement
	            {
		            Name = "PoopUpNickTitle",
		            Parent = UI_REPORT_POOPUP_PLAYER,
		            Components =
		            {
			            new CuiTextComponent
			            {
				            Text = "%TITLE_PLAYER_NICK_NAME%", Font = "robotocondensed-regular.ttf", FontSize = 14,
				            Align = TextAnchor.MiddleLeft, Color = config.ColorsSettings.AdditionalColorText
			            },
			            new CuiRectTransformComponent
			            {
				            AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-100.51 119.71", OffsetMax = "-25.96 138.81"
			            }
		            }
	            });
	            
	            container.Add(new CuiElement
	            {
		            Parent = UI_REPORT_POOPUP_PLAYER,
		            Name = "PoopUpNickName",
		            Components =
		            {
			            new CuiInputFieldComponent {  Text = "%NICK_NAME%", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleLeft, Color = config.ColorsSettings.MainColorText, NeedsKeyboard = true, LineType = InputField.LineType.SingleLine, ReadOnly = true},
			            new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-100.51 132.24", OffsetMax = "-13.00 154.29" }
		            }
	            });
		   		 		  						  	   		  	   		  	 				   		 		  				
	            container.Add(new CuiElement
	            {
		            Name = "PoopUpSteamIDTitle",
		            Parent = UI_REPORT_POOPUP_PLAYER,
		            Components =
		            {
			            new CuiTextComponent
			            {
				            Text = "%TITLE_PLAYER_STEAMID%", Font = "robotocondensed-regular.ttf", FontSize = 14,
				            Align = TextAnchor.MiddleLeft, Color = config.ColorsSettings.AdditionalColorText
			            },
			            new CuiRectTransformComponent
			            {
				            AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "34.94 119.71", OffsetMax = "155.55 138.81"
			            }
		            }
	            });
	            
	            container.Add(new CuiElement
	            {
		            Parent = UI_REPORT_POOPUP_PLAYER,
		            Name = "PoopUpSteamID",
		            Components =
		            {
			            new CuiInputFieldComponent {  Text = "%STEAMID%", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleLeft, Color = config.ColorsSettings.MainColorText, NeedsKeyboard = true, LineType = InputField.LineType.SingleLine, ReadOnly = true},
			            new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "34.93 132.24", OffsetMax = "155.54 154.29" }
		            }
	            });

	            AddInterface($"{UI_LAYER}_TEMPLATE_POOPUP", container.ToJson());
            }

             private void Building_PoopUp_Reason()
            {
	            CuiElementContainer container = new CuiElementContainer();

	            container.Add(new CuiElement
	            {
		            Name = "PoopUpReason",
		            Parent = UI_REPORT_POOPUP_PLAYER,
		            Components =
		            {
			            new CuiRawImageComponent { Color = config.ColorsSettings.AdditionalColorElements, Png = "%POOPUP_REASON%" },
			            new CuiRectTransformComponent
			            {
				            AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "%OFFSET_MIN%", OffsetMax = "%OFFSET_MAX%"
			            }
		            }
	            });
	            
	            container.Add(new CuiButton
	            {
		            Button = { Color = "0 0 0 0", Command = "%COMMAND%" },
		            Text = { Text = "%REASON%", Font = "robotocondensed-regular.ttf", FontSize = 16, Align = TextAnchor.MiddleCenter, Color = config.ColorsSettings.MainColorText },
		            RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1"}
	            }, "PoopUpReason", "ReasonButton");
		   		 		  						  	   		  	   		  	 				   		 		  				
	            AddInterface($"{UI_LAYER}_TEMPLATE_POOPUP_REASON", container.ToJson());
            }
            
            
            private void Building_PlayerTemplate_Moderator()
            {
	            CuiElementContainer container = new CuiElementContainer();
	
	            container.Add(new CuiPanel
	            {
		            CursorEnabled = false,
		            Image = { Color = "0 0 0 0" },
		            RectTransform =
		            {
			            AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "%OFFSET_MIN%", OffsetMax = "%OFFSET_MAX%"
		            }
	            }, UI_REPORT_PLAYER_PANEL, "PlayerPanel_Moderator");

	            container.Add(new CuiElement
	            {
		            Name = "AvatarPlayer",
		            Parent = "PlayerPanel_Moderator",
		            Components =
		            {
			            new CuiRawImageComponent { Color = "1 1 1 1", Png = "%AVATAR%"},
			            new CuiRectTransformComponent
			            {
				            AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-219.21 -18",
				            OffsetMax = "-183.21 18"
			            }
		            }
	            });
	            
	            container.Add(new CuiElement
	            {
		            Name = "PoopUpCircle",
		            Parent = "AvatarPlayer",
		            Components =
		            {
			            new CuiRawImageComponent { Color = config.ColorsSettings.MainColor, Png = ImageUi.GetImage(config.ImagesSettings.AvatarBlur) },
			            new CuiRectTransformComponent
			            {
				            AnchorMin = "0 0", AnchorMax = "1 1"
			            }
		            }
	            });


	            container.Add(new CuiElement
	            {
		            Name = "NickName",
		            Parent = "PlayerPanel_Moderator",
		            Components =
		            {
			            new CuiTextComponent
			            {
				            Text = "%NAME%", Font = "robotocondensed-regular.ttf", FontSize = 15,
				            Align = TextAnchor.MiddleLeft, Color = config.ColorsSettings.MainColorText
			            },
			            new CuiRectTransformComponent
			            {
				            AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-180.04 -2.93",
				            OffsetMax = "-99.62 18"
			            }
		            }
	            });

	            container.Add(new CuiElement
	            {
		            Name = "NickNameTitle",
		            Parent = "PlayerPanel_Moderator",
		            Components =
		            {
			            new CuiTextComponent
			            {
				            Text = "%TITLE_PLAYER_NICK_NAME%", Font = "robotocondensed-regular.ttf", FontSize = 16,
				            Align = TextAnchor.LowerLeft, Color = config.ColorsSettings.AdditionalColorText
			            },
			            new CuiRectTransformComponent
			            {
				            AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-180.04 -18",
				            OffsetMax = "-90.09 3.41"
			            }
		            }
	            });

	            container.Add(new CuiElement
	            {
		            Name = "SteamIDPlayer",
		            Parent = "PlayerPanel_Moderator",
		            Components =
		            {
			            new CuiTextComponent
			            {
				            Text = "%STEAMID%", Font = "robotocondensed-regular.ttf", FontSize = 15,
				            Align = TextAnchor.MiddleLeft, Color = config.ColorsSettings.MainColorText
			            },
			            new CuiRectTransformComponent
			            {
				            AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-29.44 -2.93",
				            OffsetMax = "96.62 18"
			            }
		            }
	            });

	            container.Add(new CuiElement
	            {
		            Name = "SteamIDTitle",
		            Parent = "PlayerPanel_Moderator",
		            Components =
		            {
			            new CuiTextComponent
			            {
				            Text = "%TITLE_PLAYER_STEAMID%", Font = "robotocondensed-regular.ttf", FontSize = 15,
				            Align = TextAnchor.LowerLeft, Color = config.ColorsSettings.AdditionalColorText
			            },
			            new CuiRectTransformComponent
			            {
				            AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-29.43 -18",
				            OffsetMax = "93.62 3.41"
			            }
		            }
	            });

	            container.Add(new CuiElement
	            {
		            Name = "ReportCount",
		            Parent = "PlayerPanel_Moderator",
		            Components =
		            {
			            new CuiTextComponent
			            {
				            Text = "%REPORT_COUNTS%", Font = "robotocondensed-regular.ttf", FontSize = 15,
				            Align = TextAnchor.MiddleLeft, Color = config.ColorsSettings.MainColorText
			            },
			            new CuiRectTransformComponent
			            {
				            AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "155.37 -2.93",
				            OffsetMax = "210.22 18"
			            }
		            }
	            });
		   		 		  						  	   		  	   		  	 				   		 		  				
	            container.Add(new CuiElement
	            {
		            Name = "ReportTitle",
		            Parent = "PlayerPanel_Moderator",
		            Components =
		            {
			            new CuiTextComponent
			            {
				            Text = "%TITLE_PLAYER_REPORTS%", Font = "robotocondensed-regular.ttf", FontSize = 15,
				            Align = TextAnchor.LowerLeft, Color = config.ColorsSettings.AdditionalColorText
			            },
			            new CuiRectTransformComponent
			            {
				            AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "155.37 -18",
				            OffsetMax = "210.22 3.41"
			            }
		            }
	            });
	            
	            container.Add(new CuiButton
	            { 
		            Button = { Color = "0 0 0 0", Command = "%COMMAND%" },
		            Text = { Text = "", Font = "robotocondensed-regular.ttf", FontSize = 40, Align = TextAnchor.MiddleCenter, Color = "0 0 0 0" },
		            RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1"}
	            }, "PlayerPanel_Moderator", "SelectUserForReport");

	            AddInterface($"{UI_LAYER}_TEMPLATE_PLAYER_MODERATOR", container.ToJson());
            }

            private void Building_PlayerTemplate_Moderator_IsSteam()
            {
	            CuiElementContainer container = new CuiElementContainer();
	            
	            container.Add(new CuiElement
	            {
		            Name = "IconStatusIsSteam",
		            Parent = "AvatarPlayer",
		            Components = {
			            new CuiRawImageComponent { Color = config.ColorsSettings.AdditionalColorElementsTwo, Png = "%STATUS_PLAYER%"},
			            new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-18 7.33", OffsetMax = "-7.33 18" }
		            }
	            });
	            
	            AddInterface($"{UI_LAYER}_TEMPLATE_PLAYER_MODERATOR_ISSTEAM", container.ToJson());
            }
            
            
            
            private void Building_Left_Menu()
            {
	            CuiElementContainer container = new CuiElementContainer();

	            container.Add(new CuiPanel
	            {
		            CursorEnabled = false,
		            Image = { Color = "0.80 0.29 0.29 0" },
		            RectTransform =
		            {
			            AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = "28.49 -288.17", OffsetMax = "195.39 226.15"
		            }
	            }, UI_REPORT_PANEL, UI_REPORT_LEFT_PANEL);

	            AddInterface($"{UI_LAYER}_LEFT_MENU", container.ToJson());
            }

            private void Building_Button_Template()
            {
	            CuiElementContainer container = new CuiElementContainer();

	            container.Add(new CuiElement
	            {
		            Name = "ReportPanelButton",
		            Parent = UI_REPORT_LEFT_PANEL,
		            Components =
		            {
			            new CuiRawImageComponent { Color = "%COLOR_BUTTON%", Png = ImageUi.GetImage(config.ImagesSettings.LeftBlockSettings.ButtonBackgorund) },
			            new CuiRectTransformComponent
			            {
				            AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = "%OFFSET_MIN%", OffsetMax = "%OFFSET_MAX%"
			            }
		            }
	            });
	            
	            container.Add(new CuiElement
	            {
		            Name = "LogoButtonReport",
		            Parent = "ReportPanelButton",
		            Components =
		            {
			            new CuiRawImageComponent
				            { Color = config.ColorsSettings.MainColorText, Png = "%ICON_BUTTON%" },
			            new CuiRectTransformComponent
			            {
				            AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5",  OffsetMin = "-46 -6.33", OffsetMax = "-35.33 4.33"
			            }
		            }
	            });

	            container.Add(new CuiElement
	            {
		            Name = "LabelText",
		            Parent = "ReportPanelButton",
		            Components =
		            {
			            new CuiTextComponent
			            {
				            Text = "%TITLE_BUTTON%", Font = "robotocondensed-regular.ttf", FontSize = 14,
				            Align = TextAnchor.MiddleLeft, Color = config.ColorsSettings.MainColorText
			            },
			            new CuiRectTransformComponent
			            {
				            AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-19.89 -17", OffsetMax = "62.66 17"
			            }
		            }
	            });
	            
	            container.Add(new CuiButton
	            {
		            RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
		            Button = { Command = "%COMMAND_BUTTON%", Color = "0 0 0 0" },
		            Text = { Text = "", Font = "robotocondensed-regular.ttf", FontSize = 12, Align = TextAnchor.MiddleLeft, Color = "0 0 0 0" }
	            },  "ReportPanelButton", "Button_Take");
	            
	            AddInterface($"{UI_LAYER}_LEFT_MENU_BUTTON", container.ToJson());
            }
		   		 		  						  	   		  	   		  	 				   		 		  				
            
            
            private void Building_PoopUP_Moderator()
            {
	            CuiElementContainer container = new CuiElementContainer();

	            container.Add(new CuiPanel
	            {
		            Image = { Color = "0 0 0 0.3", Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" },
		            RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-432.18 -288.17", OffsetMax = "432.34 288.29" }
	            }, UI_REPORT_PANEL, "BLURED_POOP_UP");
	            
	            container.Add(new CuiButton
	            {
		            RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
		            Button = { Close = "UI_REPORT_PANEL_CLOSE", Command = "report.panel close.poopup",  Color = "0 0 0 0" },
		            Text = { Text = "", Font = "robotocondensed-regular.ttf", FontSize = 13, Align = TextAnchor.MiddleCenter, Color = "0 0 0 0"}
	            },  "BLURED_POOP_UP", "UI_REPORT_PANEL_CLOSE");

	            container.Add(new CuiElement
	            {
		            Name = UI_REPORT_POOPUP_MODERATOR,
		            Parent = UI_REPORT_PANEL,
		            Components =
		            {
			            new CuiRawImageComponent { Color = config.ColorsSettings.MainColor, Png = ImageUi.GetImage(config.ImagesSettings.ModerationBlockSettings.ModeratorPoopUPBackgorund) },
			            new CuiRectTransformComponent
			            {
				            AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-276.59 -199.66",
				            OffsetMax = "277.40 199.66"
			            }
		            }
	            });

	            container.Add(new CuiElement
	            {
		            Name = "PoopUpAvatar",
		            Parent = UI_REPORT_POOPUP_MODERATOR,
		            Components =
		            {
			            new CuiRawImageComponent { Color = "1 1 1 1", Png = "%AVATAR%"},
			            new CuiRectTransformComponent
			            {
				            AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-235.53 111.86",
				            OffsetMax = "-192.86 154.53"
			            }
		            }
	            });

	            container.Add(new CuiElement
	            {
		            Name = "PoopUpCircle",
		            Parent = "PoopUpAvatar",
		            Components =
		            {
			            new CuiRawImageComponent { Color = config.ColorsSettings.MainColor, Png = ImageUi.GetImage(config.ImagesSettings.AvatarBlur) },
			            new CuiRectTransformComponent
			            {
				            AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-21.33 -21.33",
				            OffsetMax = "21.33 21.33"
			            }
		            }
	            });
		   		 		  						  	   		  	   		  	 				   		 		  				
	            container.Add(new CuiElement
	            {
		            Name = "PoopUpNickTitle",
		            Parent = UI_REPORT_POOPUP_MODERATOR,
		            Components =
		            {
			            new CuiTextComponent
			            {
				            Text = "%TITLE_PLAYER_NICK_NAME%", Font = "robotocondensed-regular.ttf", FontSize = 16,
				            Align = TextAnchor.MiddleLeft, Color = config.ColorsSettings.AdditionalColorText
			            },
			            new CuiRectTransformComponent
			            {
				            AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-181.08 113.19", OffsetMax = "-93.58 134.67"
			            }
		            }
	            });

	            container.Add(new CuiElement
	            {
		            Parent = UI_REPORT_POOPUP_MODERATOR,
		            Name = "PoopUpNickName",
		            Components =
		            {
			            new CuiInputFieldComponent {  Text = "%PLAYER_NAME%", Font = "robotocondensed-regular.ttf", FontSize = 16, Align = TextAnchor.UpperLeft, Color = config.ColorsSettings.MainColorText, NeedsKeyboard = true, LineType = InputField.LineType.SingleLine, ReadOnly = true},
			            new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-181.08 125.84", OffsetMax = "-93.58 147.89" }
		            }
	            });

	            container.Add(new CuiElement
	            {
		            Name = "PoopUpSteamIDTitle",
		            Parent = UI_REPORT_POOPUP_MODERATOR,
		            Components =
		            {
			            new CuiTextComponent
			            {
				            Text = "%TITLE_PLAYER_STEAMID%", Font = "robotocondensed-regular.ttf", FontSize = 16,
				            Align = TextAnchor.MiddleLeft, Color = config.ColorsSettings.AdditionalColorText
			            },
			            new CuiRectTransformComponent
			            {
				            AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-62.83 113.19",
				            OffsetMax = "57.77 134.67"
			            }
		            }
	            });
		   		 		  						  	   		  	   		  	 				   		 		  				
	            container.Add(new CuiElement
	            {
		            Parent = UI_REPORT_POOPUP_MODERATOR,
		            Name = "PoopUpSteamID",
		            Components =
		            {
			            new CuiInputFieldComponent {  Text = "%PLAYER_USERID%", Font = "robotocondensed-regular.ttf", FontSize = 16, Align = TextAnchor.UpperLeft, Color = config.ColorsSettings.MainColorText, NeedsKeyboard = true, LineType = InputField.LineType.SingleLine, ReadOnly = true},
			            new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-62.83 125.84", OffsetMax = "63.77 147.89" }
		            }
	            });

	            container.Add(new CuiElement
	            {
		            Name = "CheckStatus",
		            Parent = UI_REPORT_POOPUP_MODERATOR,
		            Components =
		            {
			            new CuiTextComponent
			            {
				            Text = "%LAST_MODER_CHECK_NAME%", Font = "robotocondensed-regular.ttf", FontSize = 13,
				            Align = TextAnchor.MiddleLeft, Color = config.ColorsSettings.MainColorText
			            },
			            new CuiRectTransformComponent
			            {
				            AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-232.80 56.22", OffsetMax = "-84.33 78.64"
			            }
		            }
	            });

	            container.Add(new CuiElement
	            {
		            Name = "CheckStatusTitle",
		            Parent = UI_REPORT_POOPUP_MODERATOR,
		            Components =
		            {
			            new CuiTextComponent
			            {
				            Text = "%TITLE_POOPUP_MODERATION_LAST_CHECK_TITLE%", Font = "robotocondensed-regular.ttf", FontSize = 13,
				            Align = TextAnchor.MiddleLeft, Color = config.ColorsSettings.AdditionalColorText
			            },
			            new CuiRectTransformComponent
			            {
				            AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-232.80 42.76", OffsetMax = "-84.32 64.84"
			            }
		            }
	            });

	            container.Add(new CuiElement
	            {
		            Name = "AmountCheck",
		            Parent = UI_REPORT_POOPUP_MODERATOR,
		            Components =
		            {
			            new CuiTextComponent
			            {
				            Text = "%AMOUNT_CHECK%", Font = "robotocondensed-regular.ttf", FontSize = 13,
				            Align = TextAnchor.MiddleLeft, Color = config.ColorsSettings.MainColorText
			            },
			            new CuiRectTransformComponent
			            {
				            AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-67.03 56.23", OffsetMax = "81.43 78.64"
			            }
		            }
	            });

	            container.Add(new CuiElement
	            {
		            Name = "AmountCheckTitle",
		            Parent = UI_REPORT_POOPUP_MODERATOR,
		            Components =
		            {
			            new CuiTextComponent
			            {
				            Text = "%TITLE_POOPUP_MODERATION_AMOUNT_CHECK_TITLE%", Font = "robotocondensed-regular.ttf", FontSize = 13,
				            Align = TextAnchor.MiddleLeft, Color = config.ColorsSettings.AdditionalColorText
			            },
			            new CuiRectTransformComponent
			            {
				            AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-67.03 42.76", OffsetMax = "81.43 64.84"
			            }
		            }
	            });

	            container.Add(new CuiElement
	            {
		            Name = "AmountReport",
		            Parent = UI_REPORT_POOPUP_MODERATOR,
		            Components =
		            {
			            new CuiTextComponent
			            {
				            Text = "%AMOUNT_REPORTS%", Font = "robotocondensed-regular.ttf", FontSize = 13,
				            Align = TextAnchor.MiddleLeft, Color = config.ColorsSettings.MainColorText
			            },
			            new CuiRectTransformComponent
			            {
				            AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "99.96 56.23", OffsetMax = "248.43 78.64"
			            }
		            }
	            });
																																																																																																			//diavel
	            container.Add(new CuiElement
	            {
		            Name = "AmountReportTitle",
		            Parent = UI_REPORT_POOPUP_MODERATOR,
		            Components =
		            {
			            new CuiTextComponent
			            {
				            Text = "%TITLE_POOPUP_MODERATION_REPORTS_TITLE%", Font = "robotocondensed-regular.ttf", FontSize = 13,
				            Align = TextAnchor.MiddleLeft, Color = config.ColorsSettings.AdditionalColorText
			            },
			            new CuiRectTransformComponent
			            {
				            AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "99.96 42.76", OffsetMax = "248.43 64.84"
			            }
		            }
	            });
	            
	            container.Add(new CuiElement
	            {
		            Name = "StartCheckPlayerButton",
		            Parent = UI_REPORT_POOPUP_MODERATOR,
		            Components = {
			            new CuiRawImageComponent { Color = config.ColorsSettings.AdditionalColorElements, Png = ImageUi.GetImage(config.ImagesSettings.LeftBlockSettings.ButtonBackgorund)},
			            new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "107.4 116.2", OffsetMax = "232.73 150.2" }
		            }
	            });
	           
	            container.Add(new CuiButton
	            {
		            RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
		            Button = { Close = UI_REPORT_PANEL, Command = "%COMMAND_START%", Color = "0 0 0 0" },
		            Text = { Text = "%TITLE_POOPUP_MODERATION_INFO_BUTTON_START_CHECK%", Font = "robotocondensed-regular.ttf", FontSize = 13, Align = TextAnchor.MiddleCenter, Color = config.ColorsSettings.MainColorText }
	            },  "StartCheckPlayerButton", "StartFuncCheckPlayer");
		   		 		  						  	   		  	   		  	 				   		 		  				
	            AddInterface($"{UI_LAYER}_POOPUP_MODERATOR", container.ToJson());
            }

            private void Building_PoopUP_Moderator_InfoBlock()
            {
	            CuiElementContainer container = new CuiElementContainer();
	            
	            container.Add(new CuiElement
	            {
		            Name = "HistoryReportsPanel",
		            Parent = UI_REPORT_POOPUP_MODERATOR,
		            Components =
		            {
			            new CuiRawImageComponent { Color = config.ColorsSettings.MainColor, Png = ImageUi.GetImage(config.ImagesSettings.ModerationBlockSettings.ModeratorPoopUPPanelBackgorund) },
			            new CuiRectTransformComponent
			            {
				            AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "%OFFSET_MIN%", OffsetMax = "%OFFSET_MAX%"
			            }
		            }
	            });
	            
	            container.Add(new CuiElement
	            {
		            Name = "TitleHistory",
		            Parent = "HistoryReportsPanel",
		            Components =
		            {
			            new CuiTextComponent
			            {
				            Text = "%TITLE_PANEL%", Font = "robotocondensed-regular.ttf", FontSize = 14,
				            Align = TextAnchor.MiddleCenter, Color = config.ColorsSettings.MainColorText
			            },
			            new CuiRectTransformComponent
			            {
				            AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-65.33 55.41",
				            OffsetMax = "65.33 80"
			            }
		            }
	            });

	            AddInterface($"{UI_LAYER}_POOPUP_MODERATOR_INFO_BLOCK", container.ToJson());
            }

            private void Building_Text_Template_Moderator_Block_Info()
            {
	            CuiElementContainer container = new CuiElementContainer();
	            
	            container.Add(new CuiElement
	            {
		            Name = "PanelText",
		            Parent = "HistoryReportsPanel",
		            Components =
		            {
			            new CuiRawImageComponent { Color = config.ColorsSettings.AdditionalColorElements, Png = ImageUi.GetImage(config.ImagesSettings.ModerationBlockSettings.ModeratorPoopUPTextBackgorund) },
			            new CuiRectTransformComponent
			            {
				            AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "%OFFSET_MIN%", OffsetMax = "%OFFSET_MAX%"
			            }
		            }
	            });

	            container.Add(new CuiElement
	            {
		            Name = "Title",
		            Parent = "PanelText",
		            Components =
		            {
			            new CuiTextComponent
			            {
				            Text = "%REASON_TITLE%", Font = "robotocondensed-regular.ttf", FontSize = 10,
				            Align = TextAnchor.MiddleCenter, Color = config.ColorsSettings.MainColorText
			            },
			            new CuiRectTransformComponent
			            {
				            AnchorMin = "0 0", AnchorMax = "1 1"
			            }
		            }
	            });

	            AddInterface($"{UI_LAYER}_POOPUP_MODERATOR_INFO_BLOCK_TEXT_TEMPLATE", container.ToJson());
            }

            
            
            private void Building_Moderator_Menu()
            {
	            CuiElementContainer container = new CuiElementContainer();

	            container.Add(new CuiElement
	            {
		            Name = UI_REPORT_MODERATOR_MENU_CHECKED,
		            Parent = "Overlay",
		            Components =
		            {
			            new CuiRawImageComponent { Color = config.ColorsSettings.MainColor, Png = ImageUi.GetImage(config.ImagesSettings.ModeratorMenuCheckedSettings.ModeratorCheckedBackground) },
			            new CuiRectTransformComponent
			            {
				            AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "208.93 17.8", OffsetMax = "413.6 116.46"
			            }
		            }
	            });

	            container.Add(new CuiElement
	            {
		            Name = "BackgroundButtonStop",
		            Parent = UI_REPORT_MODERATOR_MENU_CHECKED,
		            Components =
		            {
			            new CuiRawImageComponent
				            { Color = config.ColorsSettings.AdditionalColorElementsThree, Png = ImageUi.GetImage(config.ImagesSettings.ModeratorMenuCheckedSettings.ModeratorCheckedStopButton) },
			            new CuiRectTransformComponent
			            {
				            AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-88.6 -41.2",
				            OffsetMax = "-3.26 -14.53"
			            }
		            }
	            });
		   		 		  						  	   		  	   		  	 				   		 		  				
	            container.Add(new CuiButton
	            {
		            Button = { Color = "0 0 0 0", Command = "%COMMAND_STOP_CHECKED%"},
		            Text =
		            {
			            Text = "%TITLE_PROFILE_MODERATOR_CHECKED_MENU_TITLE_BUTTON_STOP%", Font = "robotocondensed-regular.ttf", FontSize = 12,
			            Align = TextAnchor.MiddleCenter, Color = config.ColorsSettings.MainColorText
		            },
		            RectTransform =
		            {
			            AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-42.66 -13.33",
			            OffsetMax = "42.66 13.33"
		            }
	            }, "BackgroundButtonStop", "ButtonStopCheck");

	            container.Add(new CuiElement
	            {
		            Name = "BackgroundButtonVerdict",
		            Parent = UI_REPORT_MODERATOR_MENU_CHECKED,
		            Components =
		            {
			            new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage(config.ImagesSettings.ModeratorMenuCheckedSettings.ModeratorVerdictButton) },
			            new CuiRectTransformComponent
			            {
				            AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "1.13 -41.2",
				            OffsetMax = "86.46 -14.53"
			            }
		            }
	            });

	            container.Add(new CuiElement
	            {
		            Name = "TitleMenu",
		            Parent = UI_REPORT_MODERATOR_MENU_CHECKED,
		            Components =
		            {
			            new CuiTextComponent
			            {
				            Text = "%TITLE_PROFILE_MODERATOR_CHECKED_MENU_TITLES%", Font = "robotocondensed-regular.ttf", FontSize = 14,
				            Align = TextAnchor.MiddleLeft, Color = config.ColorsSettings.MainColorText
			            },
			            new CuiRectTransformComponent
			            {
				            AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-88.6 27.38",
				            OffsetMax = "51.76 44.21"
			            }
		            }
	            });

	            AddInterface($"{UI_LAYER}_MODERATOR_MENU_CHECKED", container.ToJson());
            }

            private void Building_ModeratorMenu_Verdict_Button()
            {
	            CuiElementContainer container = new CuiElementContainer();
	            
	            container.Add(new CuiButton
	            {
		            Button = { Color = "0 0 0 0", Command = "%COMMAND_VERDICT%"},
		            Text =
		            {
			            Text = "%TITLE_PROFILE_MODERATOR_CHECKED_MENU_TITLE_BUTTON_RESULT%", Font = "robotocondensed-regular.ttf", FontSize = 12,
			            Align = TextAnchor.MiddleCenter, Color = config.ColorsSettings.MainColorText
		            },
		            RectTransform =
		            {
			            AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-42.66 -13.33",
			            OffsetMax = "42.66 13.33"
		            }
	            }, "BackgroundButtonVerdict", "ButtonVerdictCheck");

	            AddInterface($"{UI_LAYER}_MODERATOR_MENU_CHECKED_BUTTON_VERDICT", container.ToJson());
            }
            private void Building_ModeratorMenuChecked_InfoOnline()
            {
	            CuiElementContainer container = new CuiElementContainer();

	            container.Add(new CuiElement
	            {
		            Name = "InfoStatus",
		            Parent = UI_REPORT_MODERATOR_MENU_CHECKED,
		            Components =
		            {
			            new CuiTextComponent
			            {
				            Text = "%TITLE_PROFILE_MODERATOR_CHECKED_MENU_TITLE_STATUS%", Font = "robotocondensed-regular.ttf", FontSize = 10,
				            Align = TextAnchor.MiddleLeft, Color = config.ColorsSettings.MainColorText
			            },
			            new CuiRectTransformComponent
			            {
				            AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-88.6 -6.94",
				            OffsetMax = "51.76 9.88"
			            }
		            }
	            });
		   		 		  						  	   		  	   		  	 				   		 		  				
	            AddInterface($"{UI_LAYER}_ONLINE_STATUS_CHECKED_MODERATOR", container.ToJson());
            }
            private void Building_ModeratorMenuChecked_InfoDiscord()
            {
	            CuiElementContainer container = new CuiElementContainer();
	            
	            container.Add(new CuiElement
	            {
		            Parent = UI_REPORT_MODERATOR_MENU_CHECKED,
		            Name = "InfoDiscord",
		            Components =
		            {
			            new CuiInputFieldComponent {  Text = "%TITLE_PROFILE_MODERATOR_CHECKED_MENU_TITLE_DISCORD%", Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleLeft, Color = config.ColorsSettings.MainColorText, NeedsKeyboard = true, LineType = InputField.LineType.SingleLine, ReadOnly = true },
			            new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-88.6 8.45", OffsetMax = "51.76 25.28" }
		            }
	            });

	            AddInterface($"{UI_LAYER}_DISCORD_STATUS_CHECKED_MODERATOR", container.ToJson());
            }
            private void Building_DropList_Reasons()
            {
	            CuiElementContainer container = new CuiElementContainer();
	            
	            container.Add(new CuiElement
	            {
		            Name = "%REASON_NAME%",
		            Parent = "%PARENT_UI%",
		            Components =
		            {
			            new CuiRawImageComponent { Color = config.ColorsSettings.MainColor, Png = ImageUi.GetImage(config.ImagesSettings.ReasonModeratorAndRaiting) },
			            new CuiRectTransformComponent
			            {
				            AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "%OFFSET_MIN%", OffsetMax = "%OFFSET_MAX%"
			            }
		            }
	            });

	            container.Add(new CuiButton
	            {
		            Button = { Color = "1 1 1 0", Command = "%COMMAND_REASON%"},
		            Text =
		            {
			            Text = "%TEXT_TITLE%", Font = "robotocondensed-regular.ttf", FontSize = 14,
			            Align = TextAnchor.MiddleCenter, Color = config.ColorsSettings.MainColorText
		            },
		            RectTransform =
		            {
			            AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-102.33 -12", OffsetMax = "102.33 12"
		            }
	            }, "%REASON_NAME%", "SelectReason");
	            
	            AddInterface($"{UI_LAYER}_REASON_MENU_LABEL", container.ToJson());
            }

            
            
            private void Building_Raiting_Menu()
            {
	            CuiElementContainer container = new CuiElementContainer();

	            container.Add(new CuiElement
	            {
		            Name = UI_REPORT_RAITING_PLAYER_PANEL,
		            Parent = "Overlay",
		            Components =
		            {
			            new CuiRawImageComponent { Color = config.ColorsSettings.MainColor, Png = ImageUi.GetImage(config.ImagesSettings.PlayerMenuRaitingSettings.PlayerMenuRaitingBackground) },
			            new CuiRectTransformComponent
			            {
				            AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "208.93 17.8", OffsetMax = "413.6 83.13"
			            }
		            }
	            });

	            container.Add(new CuiElement
	            {
		            Name = "TitleRating",
		            Parent = UI_REPORT_RAITING_PLAYER_PANEL,
		            Components =
		            {
			            new CuiTextComponent
			            {
				            Text = "%TITLE_RAITING_WORK_MODERATOR_TITLE%", Font = "robotocondensed-regular.ttf", FontSize = 14,
				            Align = TextAnchor.UpperLeft, Color = config.ColorsSettings.MainColorText
			            },
			            new CuiRectTransformComponent
			            {
				            AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-88.24 6.37",
				            OffsetMax = "43.84 24.35"
			            }
		            }
	            });

	            container.Add(new CuiElement
	            {
		            Name = "InfoNameModer",
		            Parent = UI_REPORT_RAITING_PLAYER_PANEL,
		            Components =
		            {
			            new CuiTextComponent
			            {
				            Text = "%TITLE_RAITING_WORK_MODERATOR_WHO_MODERATOR%", Font = "robotocondensed-regular.ttf", FontSize = 10,
				            Align = TextAnchor.UpperLeft, Color = config.ColorsSettings.MainColorText
			            },
			            new CuiRectTransformComponent
			            {
				            AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-88.24 -9.69",
				            OffsetMax = "33.84 5.29"
			            }
		            }
	            });
		   		 		  						  	   		  	   		  	 				   		 		  				
	            container.Add(new CuiButton
	            {
		            Button = { Color = "0 0 0 0", Command = "%COMMAND_CLOSE%"},
		            Text =
		            {
			            Text = "%TITLE_CLOSE_BUTTON_REPORT%", Font = "robotocondensed-regular.ttf", FontSize = 14,
			            Align = TextAnchor.MiddleCenter, Color = config.ColorsSettings.MainColorText
		            },
		            RectTransform =
		            {
			            AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "77.08 9.37",
			            OffsetMax = "94.24 24.83"
		            }
	            }, UI_REPORT_RAITING_PLAYER_PANEL, "NoRaitingClose");

	            AddInterface($"{UI_LAYER}_RAITING_MENU_PLAYER", container.ToJson());
            }

            private void Building_Raiting_Select_Button()
            {
	            CuiElementContainer container = new CuiElementContainer();
	            
	            container.Add(new CuiElement
	            {
		            Name = "RaitingPng",
		            Parent = UI_REPORT_RAITING_PLAYER_PANEL,
		            Components =
		            {
			            new CuiRawImageComponent { Color = "%COLOR_STARS%", Png = ImageUi.GetImage(config.ImagesSettings.StatisticsBlockSettings.RaitingImage)},
			            new CuiRectTransformComponent
			            {
				            AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "%OFFSET_MIN%", OffsetMax = "%OFFSET_MAX%"
			            }
		            }
	            });
	            
	            container.Add(new CuiButton
	            {
		            Button = { Color = "0 0 0 0", Command = "%COMMAND_STARS%"},
		            Text =
		            {
			            Text = "", Font = "robotocondensed-regular.ttf", FontSize = 14,
			            Align = TextAnchor.MiddleCenter, Color = "0 0 0 0"
		            },
		            RectTransform =
		            {
			            AnchorMin = "0 0", AnchorMax = "1 1"
		            }
	            }, "RaitingPng", "RaitingPng_Command");

	            AddInterface($"{UI_LAYER}_RAITING_MENU_PLAYER_STARS", container.ToJson());
            }

            
            
            private void Building_Player_Alert()
            {
	            CuiElementContainer container = new CuiElementContainer();
	            
	            container.Add(new CuiElement
	            {
		            Name = UI_REPORT_PLAYER_ALERT,
		            Parent = "Overlay",
		            Components = {
			            new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage(config.ImagesSettings.PlayerAlerts) },
			            new CuiRectTransformComponent { AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = "-483.33 -372.66", OffsetMax = "483.33 0.00" }
		            }
	            });
	            
	            container.Add(new CuiElement
	            {
		            Name = "LabelInfo",
		            Parent = UI_REPORT_PLAYER_ALERT,
		            Components = {
			            new CuiTextComponent { Text = "%TITLE_TEXT%", Font = "robotocondensed-regular.ttf", FontSize = 48, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
			            new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 137.23", OffsetMax = "0 -175.89" },
			            new CuiOutlineComponent { Color = "0 0 0 1", Distance = "-0.5 0.5" }
		            }
	            });

	            container.Add(new CuiElement
	            {
		            Name = "LabelDescription",
		            Parent = UI_REPORT_PLAYER_ALERT,
		            Components = {
			            new CuiTextComponent { Text = "%DESCRIPTION_TEXT%", Font = "robotocondensed-regular.ttf", FontSize = 20, Align = TextAnchor.UpperCenter, Color = "1 1 1 1" },
			            new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 39.67", OffsetMax = "0 -228.20" },
			            new CuiOutlineComponent { Color = "0 0 0 1", Distance = "-0.5 0.5" }
		            }
	            });

	            AddInterface($"{UI_LAYER}_PLAYER_ALERT", container.ToJson());
            }

            		}
		public Boolean HasImage(String imageName) => (Boolean)ImageLibrary?.Call("HasImage", imageName);
		
		void OnPlayerBanned(string name, ulong id, string address, string reason) => StopCheckedPlayer(id, null, IsConsole: true);
		
		private void DrawUI_ModeratorStitistics_Banner_RaitingImage(BasePlayer Moderator, Int32 X)
		{
			String Interface = InterfaceBuilder.GetInterface($"{InterfaceBuilder.UI_LAYER}_PROFILE_BANNER_TEMPLATE_ADDITIONAL_IMG");
			if (Interface == null) return;
			
			Interface = Interface.Replace("%OFFSET_MIN%", $"{-59.85 + (X * 12)} -13.93");
			Interface = Interface.Replace("%OFFSET_MAX%", $"{-49.18 + (X * 12)} -3.93");

			CuiHelper.AddUi(Moderator, Interface);
		}

		private List<String> GetServersCheckRCC(UInt64 TargetID)
		{
			if (String.IsNullOrWhiteSpace(config.ReferenceSettings.RCCSettings.RCCKey)) return null;
			return !RCC_LocalRepository.ContainsKey(TargetID) ? new List<String>() : RCC_LocalRepository[TargetID].LastChecksServers;
		}
		private void DrawUI_PoopUp_Moderator_InfoText(BasePlayer player, Int32 Y, String Text)
		{
			String Interface = InterfaceBuilder.GetInterface($"{InterfaceBuilder.UI_LAYER}_POOPUP_MODERATOR_INFO_BLOCK_TEXT_TEMPLATE");
			if (Interface == null) return;
			
			Interface = Interface.Replace("%REASON_TITLE%", Text);
			Interface = Interface.Replace("%OFFSET_MIN%", $"-51.66 {35.13 - (Y * 20)}");
			Interface = Interface.Replace("%OFFSET_MAX%", $"51.66 {49.8 - (Y * 20)}");

			CuiHelper.AddUi(player, Interface);
		}
		
		
		
		private void DrawUI_PageController(BasePlayer player, Int32 Page, Boolean IsModerator, String SearchName = "")
		{
			List<FakePlayer> fakePlayers = null;
			List<BasePlayer> playerList = null;
			Int32 AllCountPlayers = 0;
			Int32 MaxPlayerPage = IsModerator ? 7 : 27;

			if (IsModerator)  //
			{  
				List<BasePlayer> moderatorSortedList = BasePlayer.activePlayerList.Where(x => x.userID != player.userID && !permission.UserHasPermission(x.UserIDString, HideMenuPermissions) && !PlayerChecks.ContainsKey(x.userID) && PlayerInformations[x.userID].Reports >= config.ReportContollerModerationSettings.ReportCountTrigger && x.displayName.ToLower().Contains(SearchName.ToLower())).OrderByDescending(x => PlayerInformations[x.userID].Reports).ToList();
				playerList = moderatorSortedList.Skip(Page * MaxPlayerPage).Take(MaxPlayerPage).ToList();

				AllCountPlayers = moderatorSortedList.Count;
			}
			else
			{
				if (IQFakeActive && config.ReferenceSettings.IQFakeActiveUse)
				{
					List<FakePlayer> fakePlayersSorted = PlayerBases.Where(x => x.UserID != player.userID && !permission.UserHasPermission(x.UserID.ToString(), HideMenuPermissions) && x.DisplayName.ToLower().Contains(SearchName.ToLower())).ToList();
					fakePlayers = fakePlayersSorted.Skip(Page * MaxPlayerPage).Take(MaxPlayerPage).ToList();
					
					AllCountPlayers = fakePlayersSorted.Count;
				}
				else // 
				{
					List<BasePlayer> sortedPlayers = BasePlayer.activePlayerList.Where(x => x.userID != player.userID && !permission.UserHasPermission(x.UserIDString, HideMenuPermissions) && x.displayName.ToLower().Contains(SearchName.ToLower()) && (!IsFriendSendReport(player.userID, x.userID) || !IsClansSendReport(player.UserIDString, x.UserIDString))).ToList();
					playerList = sortedPlayers.Skip(Page * MaxPlayerPage).Take(MaxPlayerPage).ToList();
					
					AllCountPlayers = sortedPlayers.Count;
				}
			}

			String Interface = InterfaceBuilder.GetInterface($"{InterfaceBuilder.UI_LAYER}_PANEL_PLAYERS_PAGE_CONTROLLER");
            if (Interface == null) return;
            
            Interface = Interface.Replace("%AMOUNT_PAGE%", $"{Page}");																		
            Interface = Interface.Replace("%COMMAND_PAGE_NEXT%", AllCountPlayers < MaxPlayerPage ? "" : $"report.panel page.controller {(AllCountPlayers >= (Page + 1) * MaxPlayerPage ? (Page + 1) : 0)} {IsModerator}");
            Interface = Interface.Replace("%COMMAND_PAGE_BACK%", AllCountPlayers < MaxPlayerPage ? "" : $"report.panel page.controller {(Page <= 0 ? AllCountPlayers / MaxPlayerPage : Page - 1)} {IsModerator}");

            CuiHelper.DestroyUi(player, "ActionPageCount");
            CuiHelper.DestroyUi(player, "ButtonNextPage");
            CuiHelper.DestroyUi(player, "ButtonBackPage");
            CuiHelper.AddUi(player, Interface);

            ShowPlayersList(player, playerList, fakePlayers, IsModerator);
        }

        
        private void ChatCommandDiscord(BasePlayer player, String cmd, String[] args)
        {
	        String Discord = String.Join(" ", args);
	        SendPlayerDiscord(player, Discord);
        }
        
                
        private void SendVK(String Message)
        {
	        if (String.IsNullOrWhiteSpace(config.NotifyVKSettings.VKTokenGroup) || String.IsNullOrWhiteSpace(config.NotifyVKSettings.VKChatID)) return;
	        
	        while (Message.Contains("#"))
		        Message = Message.Replace("#", "%23");
	        
	        RequestVK(Message);
        }
		private Boolean IsClansStartChecked(String userID, String targetID) => config.ReferenceSettings.ClansSetting.StartCheckedClan && IsClans(userID, targetID);

		internal class LocalRepositoryRCC
		{
			public List<String> LastChecksServers = new List<String>();
			public List<String> LastBansServers = new List<String>();
		}
		private new void LoadDefaultMessages()
		{
			lang.RegisterMessages(new Dictionary<string, string>
			{
				["TITLE_NAME_REPORT_SYSTEM"] = "<b>IQReportSystem.</b>",
				["TITLE_CLOSE_BUTTON_REPORT"] = "<b>X</b>",
				["TITLE_PLAYER_LIST"] = "<b>Players list</b>",
				["TITLE_PLAYER_NICK_NAME"] = "NICK NAME",
				["TITLE_PLAYER_STEAMID"] = "STEAMID",
				["TITLE_PLAYER_REPORTS"] = "REPORTS",
				["TITLE_LEFT_MENU_BUTTON_REPORTS"] = "Reports",
				["TITLE_LEFT_MENU_BUTTON_MODERATION"] = "Moderations",
				["TITLE_POOPUP_MODERATION_LAST_CHECK_TITLE"] = "Last verifier",
				["TITLE_POOPUP_MODERATION_AMOUNT_CHECK_TITLE"] = "Number of checks",
				["TITLE_POOPUP_MODERATION_REPORTS_TITLE"] = "Reports",
				["TITLE_POOPUP_MODERATION_NO_CHECKED"] = "Has not verified",
				["TITLE_POOPUP_MODERATION_HISTORY_REPORTS_TITLE"] = "History reports",
				["TITLE_POOPUP_MODERATION_INFO_BLOCK_EMPTY"] = "Information empty",
				["TITLE_POOPUP_MODERATION_INFO_CHECK_SERVERS_RCC"] = "Checkeds",
				["TITLE_POOPUP_MODERATION_INFO_BANS_SERVERS_RCC"] = "Banneds",
				["TITLE_POOPUP_MODERATION_INFO_BANS_SERVERS_OZPROTECT"] = "Banneds",
				["TITLE_POOPUP_MODERATION_INFO_BUTTON_START_CHECK"] = "<b>CHECK</b>",
				["TITLE_PLAYER_HEADER_TITLE_SEND_REPORT"] = "<b>Send report</b>",
				["TITLE_PLAYER_HEADER_TITLE_DESC_SEND_REPORT"] = "Select a player to",
				["TITLE_PLAYER_HEADER_TITLE_SEARCH_PLAYER"] = "Find players",
				["TITLE_PROFILE_INFO_CHECKED"] = "Checked : {0}",
				["TITLE_PROFILE_MODERATOR_STATISTICS_TITLE_CHECKED"] = "Checkeds",
				["TITLE_PROFILE_MODERATOR_STATISTICS_TITLE"] = "Statistics",
				["TITLE_PROFILE_MODERATOR_STATISTICS_TITLE_QUALITY_ASSESSMENT"] = "Quality assessment",
				["TITLE_PROFILE_MODERATOR_STATISTICS_TITLE_BANS"] = "Banneds",
				["TITLE_POOPUP_MODERATION_INFO_TEAMS_NAME_PLAYER"] = "Teammates",
				["TITLE_PROFILE_MODERATOR_STATISTICS_TITLE_ALLSCORE"] = "Raiting",
				["TITLE_PROFILE_MODERATOR_STATISTICS_TITLE_ONE_ACHIVE"] = "Communicative",
				["TITLE_PROFILE_MODERATOR_STATISTICS_TITLE_TWO_ACHIVE"] = "Competent",
				["TITLE_PROFILE_MODERATOR_STATISTICS_TITLE_THREE_ACHIVE"] = "Fast",
				["TITLE_PROFILE_MODERATOR_STATISTICS_DESCRIPTION_BANS_AND_CHECHKED"] = "all time",
				["TITLE_PROFILE_MODERATOR_STATISTICS_DESCRIPTION_ONE_ACHIVE"] = "don't be rude",
				["TITLE_PROFILE_MODERATOR_STATISTICS_DESCRIPTION_TWO_ACHIVE"] = "prove yourself",
				["TITLE_PROFILE_MODERATOR_STATISTICS_DESCRIPTION_THREE_ACHIVE"] = "don't delay",
				["TITLE_PROFILE_MODERATOR_CHECKED_MENU_TITLE_BUTTON_RESULT"] = "VERDICT",
				["TITLE_PROFILE_MODERATOR_CHECKED_MENU_TITLES"] = "Reviewer menu",
				["TITLE_PROFILE_MODERATOR_CHECKED_MENU_TITLE_BUTTON_STOP"] = "STOP",
				["TITLE_PROFILE_MODERATOR_CHECKED_MENU_TITLE_STATUS"] = "STATUS : {0}",
				["TITLE_PROFILE_MODERATOR_CHECKED_MENU_TITLE_STATUS_DEFAULT"] = "ONLINE",
				["TITLE_PROFILE_MODERATOR_CHECKED_MENU_TITLE_DISCORD"] = "DISCORD : {0}",
				["TITLE_PROFILE_MODERATOR_CHECKED_MENU_TITLE_DISCORD_EMPTY"] = "NOT PROVIDED",
				["TITLE_RAITING_WORK_MODERATOR_TITLE"] = "Evaluate the reviewer",
				["TITLE_RAITING_WORK_MODERATOR_WHO_MODERATOR"] = "You were checked : {0}",
				["TITLE_RAITING_WORK_MODERATOR_WHO_MODERATOR_NOT_NAME"] = "MODERATOR",
				["FUNCIONAL_MESSAGE_NO_SEND_RAITING_FOR_MODERATOR"] = "The player refrained from evaluating your check",
				["TITLE_PLAYER_ALERT_INFORMATION_TITLE"] = "<b><size=34><color=#70C3F8>YOU HAVE BEEN CALLED FOR A CHECK</color></size></b>",
				["TITLE_PLAYER_ALERT_INFORMATION_DESCRIPTION"] = "<b><size=14>You have exceeded the maximum allowable number of reports!" +
				                                                 "\nProvide your <color=#70C3F8>Discord</color> for our moderation to contact you." +
				                                                 "\nIn case of <color=#70C3F8>ignoring</color> this message - you will receive <color=#70C3F8>banneds</color> on the server!" +
				                                                 "\n\nCommand to send : <color=#70C3F8>/discord YourName</color></size></b>",
				
				["FUNCIONAL_MESSAGE_CHECK_AFK_STARTING"] = "We start checking the player on AFK",
				["FUNCIONAL_MESSAGE_CHECK_AFK_TRY"] = "Checking a player for AFK\nAttemp : {0}",
				["FUNCIONAL_MESSAGE_CHECK_START"] = "The check is started, the player is notified!",
				["FUNCIONAL_MESSAGE_CHECK_AFK_PLAYER_AFK"] = "Check has not been started!\nThe player is in AFK, please try again later",
				["FUNCIONAL_MESSAGE_CHECK_AFK_PLAYER_LEAVE"] = "Player left the server while checking for AFK\nChecking cancelled, please try again later",
				["FUNCIONAL_MESSAGE_CHECK_AFK_PLAYER_SEND_CHAT"] = "The player wrote a message in the chat\nMessage : {0}",
				["FUNCIONAL_MESSAGE_CHECK_AFK_PLAYER_SEND_COMMAND"] = "The player used the command\nCommand : {0}",
				["FUNCIONAL_MESSAGE_CHECK_AFK_PLAYER_START_CRAFTING"] = "The player has just crafted an item",
				["FUNCIONAL_MESSAGE_CHECK_AFK_PLAYER_CANCELLED_CRAFTING"] = "Player just canceled item crafting",
				["FUNCIONAL_MESSAGE_NO_SEND_RAITING"] = "You refrained from rating the moderator",
				["FUNCIONAL_MESSAGE_SEND_RAITING"] = "Thanks!\nYour rating has been successfully submitted\nWe care about the quality of moderation work",
				["FUNCIONAL_MESSAGE_SEND_RAITING_FOR_MODERATOR"] = "The player has rated your check",
				["FUNCIONAL_SEND_DISCORD_NULL_DS"] = "You have not entered your <color=#70C3F8>Discord</color>",
				["FUNCIONAL_SEND_DISCORD_NO_REGEX_DS"] = "Enter correct <color=#70C3F8>Discord</color>",
				["FUNCIONAL_SEND_DISCORD_SUCCESS"] = "You have successfully submitted your <color=#70C3F8>Discord</color>\nContact : {0}",
				["FUNCIONAL_SEND_DISCORD_SUCCESS_ALERT_MODERATOR"] = "The player sent you his <color=#70C3F8>Discord</color>\nContact : {0}",
				["FUNCIONAL_CHANGE_STATUS_PLAYER_ALERT_MODERATOR"] = "The player's connection status has changed from server to : {0}\nWait for it for 15 minutes, otherwise issue a block for refusing to check",
				["FUNCIONAL_CHANGE_STATUS_PLAYER_ONLINE_ALERT_MODERATOR"] = "The player has connected to the server\nCheck continued",
				["FUNCIONAL_CHANGE_STATUS_PLAYER_ONLINE_ALERT_PLAYER"] = "Connected!\nChecking continued, the moderator is waiting for your contact!",
				["FUNCIONAL_CHANGE_STATUS_MODERATOR_DISCONNECTED"] = "The moderator has left the server\nWait 10 minutes - verification will continue after it is connected\nOtherwise, the check will be canceled automatically.",
				["FUNCIONAL_CHANGE_STATUS_MODERATOR_RECONNECTED"] = "Moderator reconnected to the server\nChecked continued",
				["FUNCIONAL_CHANGE_STATUS_MODERATOR_DISCONNECTED_FULL_LEAVE"] = "Moderator failed to connect\nCheck has been canceled\nWe're sorry, have fun",
				["FUNCIONAL_MODERATOR_VERDICT_RESULT"] = "You finished checking with a verdict : {0}\nAppropriate action has been taken against the player",
				["FUNCIONAL_SEND_REPORT_SUCCESS"] = "You have successfully reported player {0}",
				["FUNCIONAL_NO_DUPLE_SEND_REPORT"] = "You have already sent a complaint about this player!\nWait for it to be checked",
				["FUNCIONAL_COOLDOWN_REPORT"] = "You have already sent a complaint!\nWait {0} seconds before resending",
				["FUNCIONAL_PLAYER_STOP_DAMAGE_MAN_ADD"] = "Damage is disabled for you during the check - you can't deal damage to players, buildings, etc.",

				["NOTIFY_PLAYERS_START_CHECK_MODERATOR"] = "Moderator {0} called player {1} to check\nYou can send a report using /report",
				["NOTIFY_PLAYERS_START_CHECK_NOT_MODERATOR"] = "Player {0} was called for check\nYou can send a report using /report",
				["NOTIFY_PLAYERS_STOP_CHECK_MODERATOR"] = "Moderator {0} finished checking player {1}\nUse of prohibited software - not detected",
				["NOTIFY_PLAYERS_STOP_CHECK_NOT_MODERATOR"] = "Checking of player {0} completed\nUse of prohibited software - not detected",
				["NOTIFY_PLAYERS_STOP_CHECK_VERDICT_MODERATOR"] = "Moderator {0} finished checking player {1} with verdict {2}\nAppropriate action has been taken against the player.",
				["NOTIFY_PLAYERS_STOP_CHECK_VERDICT_NOT_MODERATOR"] = "Verification of player {0} completed with verdict {1}\nAppropriate action has been taken against the player.",
				["NOTIFY_MODERATOR_RAIDBLOCK_PLAYER"] = "The check was canceled automatically with complaints saved\nReason : the player has an active raid-block",
				["NOTIFY_MODERATOR_ITS_PLAYER_CHECKED"] = "This player has already been called for checked",
				["NOTIFY_MODERATOR_COMBATBLOCK_PLAYER"] = "The check was canceled automatically with complaints saved\nReason : the player has an active combat-block",
				["NOTIFY_MODERATOR_DUEL_PLAYER"] = "The check was canceled automatically with complaints saved\nReason : the player is in a duel",
				["NOTIFY_MODERATOR_FRIEND_PLAYER"] = "The check was canceled automatically with complaints saved\nReason : the player is your teammate",
				["NOTIFY_MODERATOR_MAX_REPORT"] = "Player {0} has exceeded the number of reports, call him for check!\nNumber of reports : {1}",

			}, this);
			
			lang.RegisterMessages(new Dictionary<string, string>
			{
				["TITLE_NAME_REPORT_SYSTEM"] = "<b>IQReportSystem.</b>",
				["TITLE_CLOSE_BUTTON_REPORT"] = "<b>X</b>",
				["TITLE_PLAYER_LIST"] = "<b>Список игроков</b>",
				["TITLE_PLAYER_NICK_NAME"] = "NICK NAME",
				["TITLE_PLAYER_STEAMID"] = "STEAMID",
				["TITLE_PLAYER_REPORTS"] = "ЖАЛОБ",
				["TITLE_LEFT_MENU_BUTTON_REPORTS"] = "Жалобы",
				["TITLE_LEFT_MENU_BUTTON_MODERATION"] = "Модерация",
				["TITLE_POOPUP_MODERATION_LAST_CHECK_TITLE"] = "Последний проверяющий",
				["TITLE_POOPUP_MODERATION_AMOUNT_CHECK_TITLE"] = "Количество проверок",
				["TITLE_POOPUP_MODERATION_REPORTS_TITLE"] = "Жалоб(/ы)",
				["TITLE_POOPUP_MODERATION_NO_CHECKED"] = "Не был проверен",
				["TITLE_POOPUP_MODERATION_HISTORY_REPORTS_TITLE"] = "История жалоб",
				["TITLE_POOPUP_MODERATION_INFO_BLOCK_EMPTY"] = "Информации нет",
				["TITLE_POOPUP_MODERATION_INFO_CHECK_SERVERS_RCC"] = "Проверялся",
				["TITLE_POOPUP_MODERATION_INFO_BANS_SERVERS_RCC"] = "Забанен",
				["TITLE_POOPUP_MODERATION_INFO_BANS_SERVERS_OZPROTECT"] = "Забанен",
				["TITLE_POOPUP_MODERATION_INFO_TEAMS_NAME_PLAYER"] = "Тиммейты",
				["TITLE_POOPUP_MODERATION_INFO_BUTTON_START_CHECK"] = "<b>ПРОВЕРИТЬ</b>",
				["TITLE_PLAYER_HEADER_TITLE_SEND_REPORT"] = "<b>Отправить жалобу</b>",
				["TITLE_PLAYER_HEADER_TITLE_DESC_SEND_REPORT"] = "Выберите игрока чтобы",
				["TITLE_PLAYER_HEADER_TITLE_SEARCH_PLAYER"] = "Поиск игроков",
				["TITLE_PROFILE_INFO_CHECKED"] = "Проверен : {0}",
				["TITLE_PROFILE_MODERATOR_STATISTICS_TITLE_CHECKED"] = "Проверок",
				["TITLE_PROFILE_MODERATOR_STATISTICS_TITLE"] = "Статистика",
				["TITLE_PROFILE_MODERATOR_STATISTICS_TITLE_QUALITY_ASSESSMENT"] = "Оценка качества",
				["TITLE_PROFILE_MODERATOR_STATISTICS_TITLE_BANS"] = "Блокировок",
				["TITLE_PROFILE_MODERATOR_STATISTICS_TITLE_ALLSCORE"] = "Общий балл",
				["TITLE_PROFILE_MODERATOR_STATISTICS_TITLE_ONE_ACHIVE"] = "Общительный", 
				["TITLE_PROFILE_MODERATOR_STATISTICS_TITLE_TWO_ACHIVE"] = "Компетентный",
				["TITLE_PROFILE_MODERATOR_STATISTICS_TITLE_THREE_ACHIVE"] = "Быстрый",
				["TITLE_PROFILE_MODERATOR_STATISTICS_DESCRIPTION_BANS_AND_CHECHKED"] = "за все время",
				["TITLE_PROFILE_MODERATOR_STATISTICS_DESCRIPTION_ONE_ACHIVE"] = "не грубите",
				["TITLE_PROFILE_MODERATOR_STATISTICS_DESCRIPTION_TWO_ACHIVE"] = "проявите себя",
				["TITLE_PROFILE_MODERATOR_STATISTICS_DESCRIPTION_THREE_ACHIVE"] = "не медлите",
				["TITLE_PROFILE_MODERATOR_CHECKED_MENU_TITLE_BUTTON_RESULT"] = "ВЕРДИКТ",
				["TITLE_PROFILE_MODERATOR_CHECKED_MENU_TITLES"] = "Меню проверяющего",
				["TITLE_PROFILE_MODERATOR_CHECKED_MENU_TITLE_BUTTON_STOP"] = "CТОП",
				["TITLE_PROFILE_MODERATOR_CHECKED_MENU_TITLE_STATUS"] = "STATUS : {0}",
				["TITLE_PROFILE_MODERATOR_CHECKED_MENU_TITLE_STATUS_DEFAULT"] = "ONLINE",
				["TITLE_PROFILE_MODERATOR_CHECKED_MENU_TITLE_DISCORD"] = "DISCORD : {0}",
				["TITLE_PROFILE_MODERATOR_CHECKED_MENU_TITLE_DISCORD_EMPTY"] = "НЕ ПРЕДОСТАВЛЕН",
				["TITLE_RAITING_WORK_MODERATOR_TITLE"] = "Оцените проверяющего",
				["TITLE_RAITING_WORK_MODERATOR_WHO_MODERATOR"] = "Вас проверял : {0}",
				["TITLE_RAITING_WORK_MODERATOR_WHO_MODERATOR_NOT_NAME"] = "МОДЕРАТОР",
				["TITLE_PLAYER_ALERT_INFORMATION_TITLE"] = "<b><size=34><color=#70C3F8>ВАС ВЫЗВАЛИ НА ПРОВЕРКУ</color></size></b>",
				["TITLE_PLAYER_ALERT_INFORMATION_DESCRIPTION"] = "<b><size=14>Вы превысили максимально-допустимое количество жалоб!" +
				                                                 "\nПредоставьте ваш <color=#70C3F8>Discord</color> для того чтобы с вами связалась наша модерация." +
				                                                 "\nВ случае <color=#70C3F8>игнорирования</color> данного сообщения - вы получите <color=#70C3F8>блокировку</color> на сервере!" +
				                                                 "\n\nКоманда для отправки : <color=#70C3F8>/discord YourName#0000</color></size></b>",
		   		 		  						  	   		  	   		  	 				   		 		  				
				["FUNCIONAL_MESSAGE_CHECK_AFK_STARTING"] = "Начинаем проверку игрока на AFK",
				["FUNCIONAL_MESSAGE_CHECK_AFK_TRY"] = "Проверяем игрока на AFK\nПопытка : {0}",
				["FUNCIONAL_MESSAGE_CHECK_START"] = "Проверка запущена, игрок уведомлен!",
				["FUNCIONAL_MESSAGE_CHECK_AFK_PLAYER_AFK"] = "Проверка не была запущена!\nИгрок находится в AFK, пожалуйста повторите попытку позже",
				["FUNCIONAL_MESSAGE_CHECK_AFK_PLAYER_LEAVE"] = "Игрок покинул сервер на стадии проверки на AFK\nПроверка отменена, повторите попытку позже",
				["FUNCIONAL_MESSAGE_CHECK_AFK_PLAYER_SEND_CHAT"] = "Игрок написал сообщение в чат\nСообщение : {0}",
				["FUNCIONAL_MESSAGE_CHECK_AFK_PLAYER_SEND_COMMAND"] = "Игрок использовал команду\nКоманда : {0}",
				["FUNCIONAL_MESSAGE_CHECK_AFK_PLAYER_START_CRAFTING"] = "Игрок только что поставил предмет на крафт",
				["FUNCIONAL_MESSAGE_CHECK_AFK_PLAYER_CANCELLED_CRAFTING"] = "Игрок только что отменил крафт предмета",
				["FUNCIONAL_MESSAGE_NO_SEND_RAITING"] = "Вы воздержались от оценки модератора",
				["FUNCIONAL_MESSAGE_NO_SEND_RAITING_FOR_MODERATOR"] = "Игрок воздержался от оценки вашей проверки",
				["FUNCIONAL_MESSAGE_SEND_RAITING"] = "Спасибо!\nВаша оценка успешно отправлена\nНам важно качество работы модерации",
				["FUNCIONAL_MESSAGE_SEND_RAITING_FOR_MODERATOR"] = "Игрок оценил вашу проверку",
				["FUNCIONAL_SEND_DISCORD_NULL_DS"] = "Вы не ввели свой <color=#70C3F8>Discord</color>",
				["FUNCIONAL_SEND_DISCORD_NO_REGEX_DS"] = "Введите корректный <color=#70C3F8>Discord</color>",
				["FUNCIONAL_SEND_DISCORD_SUCCESS"] = "Вы успешно отправили свой <color=#70C3F8>Discord</color>\nКонтакт : {0}",
				["FUNCIONAL_SEND_DISCORD_SUCCESS_ALERT_MODERATOR"] = "Игрок прислал вам свой <color=#70C3F8>Discord</color>\nКонтакт : {0}",
				["FUNCIONAL_CHANGE_STATUS_PLAYER_ALERT_MODERATOR"] = "У игрока изменился статус соединения с сервером на : {0}\nОжидайте его 15 минут, в ином случае выдавайте блокировку за отказ от проверки",
				["FUNCIONAL_CHANGE_STATUS_PLAYER_ONLINE_ALERT_MODERATOR"] = "Игрок подключился к серверу\nПроверка продолжена",
				["FUNCIONAL_CHANGE_STATUS_PLAYER_ONLINE_ALERT_PLAYER"] = "С подключением!\nПроверка продолжена, модератор ожидает ваш контакт!",
				["FUNCIONAL_CHANGE_STATUS_MODERATOR_DISCONNECTED"] = "Модератор покинул сервер\nОжидайте 10 минут - проверка продолжится после его подключения\nВ ином случае проверка будет отменена автоматически",
				["FUNCIONAL_CHANGE_STATUS_MODERATOR_DISCONNECTED_FULL_LEAVE"] = "Модератор не успел подключиться\nПроверка была отменена\nПриносим извинения, приятной игры",
				["FUNCIONAL_CHANGE_STATUS_MODERATOR_RECONNECTED"] = "Модератор переподключился на сервер\nПроверка продолжена",
				["FUNCIONAL_MODERATOR_VERDICT_RESULT"] = "Вы закончили проверку с вердиктом : {0}\nК игроку применены соотвествующие меры",
				["FUNCIONAL_SEND_REPORT_SUCCESS"] = "Вы успешно отправили жалобу на игрока {0}",
				["FUNCIONAL_NO_DUPLE_SEND_REPORT"] = "Вы уже отправляли жалобу на этого игрока!\nОжидайте когда его проверят",
				["FUNCIONAL_COOLDOWN_REPORT"] = "Вы уже отправляли жалобу!\nОжидайте {0} секунд перед повторной отправкой",
				["FUNCIONAL_PLAYER_STOP_DAMAGE_MAN_ADD"] = "На время проверки вам отключен урон - вы не можете наносить урон по игрокам, постройкам и т.д",
				
				["NOTIFY_PLAYERS_START_CHECK_MODERATOR"] = "Модератор {0} вызвал на проверку игрока {1}\nОтправить жалобу можно с помощью /report",
				["NOTIFY_PLAYERS_START_CHECK_NOT_MODERATOR"] = "Игрока {0} вызвали на проверку\nОтправить жалобу можно с помощью /report",
				["NOTIFY_PLAYERS_STOP_CHECK_MODERATOR"] = "Модератор {0} завершил проверку игрока {1}\nИспользование запрещенного ПО - не обнаружено",
				["NOTIFY_PLAYERS_STOP_CHECK_NOT_MODERATOR"] = "Проверка игрока {0} завершена\nИспользование запрещенного ПО - не обнаружено",
				["NOTIFY_PLAYERS_STOP_CHECK_VERDICT_MODERATOR"] = "Модератор {0} завершил проверку игрока {1} с вердиктом {2}\nК игроку применены соотвествующие меры",
				["NOTIFY_PLAYERS_STOP_CHECK_VERDICT_NOT_MODERATOR"] = "Проверка игрока {0} завершена с вердиктом {1}\nК игроку применены соотвествующие меры",
				["NOTIFY_MODERATOR_RAIDBLOCK_PLAYER"] = "Проверка отменена автоматически с сохранением жалоб\nПричина : у игрока активный рейд-блок",
				["NOTIFY_MODERATOR_ITS_PLAYER_CHECKED"] = "Данного игрока уже вызвали на проверку",
				["NOTIFY_MODERATOR_COMBATBLOCK_PLAYER"] = "Проверка отменена автоматически с сохранением жалоб\nПричина : у игрока активный комбат-блок",
				["NOTIFY_MODERATOR_DUEL_PLAYER"] = "Проверка отменена автоматически с сохранением жалоб\nПричина : игрок находится на дуэли",
				["NOTIFY_MODERATOR_FRIEND_PLAYER"] = "Проверка отменена автоматически с сохранением жалоб\nПричина : игрок является вашим тиммейтом",
				["NOTIFY_MODERATOR_MAX_REPORT"] = "Игрок {0} превысил количество репортов, вызовите его на проверку!\nКоличество репортов : {1}",

			}, this, "ru");
		}
		   		 		  						  	   		  	   		  	 				   		 		  				
        private void RequestVK(String Message)
        {
	        try
	        {
		        webrequest.Enqueue(GetUrlVK(Message), null, (code, response) => { }, this, RequestMethod.GET, timeout: 10F);
	        }
	        catch (Exception ex)
	        {
		        PrintWarning(LanguageEn ? "Check the correctness of the entered data for VK! Vkontakte returns an error of your data!" : "Проверьте корректность введенных данных для ВК! Вконтакте возвращает ошибку ваших данных!");
	        }
        }
        private static Double CurrentTime => Facepunch.Math.Epoch.Current;
        private void OnServerInitialized()
        {
	        PreLoadedPlugin();
            StartPluginLoad();

            if (IQFakeActive && config.ReferenceSettings.IQFakeActiveUse)
            {
	            foreach (FakePlayer fakePlayer in PlayerBases)
		            SteamAvatarAdd(fakePlayer.UserID.ToString());
            }
		   		 		  						  	   		  	   		  	 				   		 		  				
            permission.RegisterPermission(ModeratorPermissions, this);
            permission.RegisterPermission(HideMenuPermissions, this);
            
            if (!config.CheckControllerSettings.UseSoundAlert) return;
			
            LoadDataSound("ALERT_REPORT_EN");
            LoadDataSound("ALERT_REPORT_RU");
        }

		internal class LocalRepositoryOzProtect
		{
			public List<String> LastBansServers = new List<String>();
		}

                
        
        private void SendReportPlayer(BasePlayer Sender, UInt64 TargetID, Int32 ReasonIndex)
        {
	        CuiHelper.DestroyUi(Sender, "BLURED_POOP_UP");
	        CuiHelper.DestroyUi(Sender, InterfaceBuilder.UI_REPORT_POOPUP_PLAYER);
	        
	        if (PlayerRepositories.ContainsKey(Sender.userID))
	        {
		        if (PlayerRepositories[Sender.userID].IsRepeatReported(TargetID))
		        {
			        SendChat(GetLang("FUNCIONAL_NO_DUPLE_SEND_REPORT", Sender.UserIDString), Sender);
			        return;
		        }

		        if (PlayerRepositories[Sender.userID].IsCooldown(TargetID))
		        {
			        SendChat(GetLang("FUNCIONAL_COOLDOWN_REPORT", Sender.UserIDString, PlayerRepositories[Sender.userID].GetCooldownLeft(TargetID)), Sender);
			        return;
		        }
	        }

	        PlayerInformations[Sender.userID].SendReports++;

	        PlayerInformations[TargetID].Reports++;
	        
	        Configuration.ReasonReport Reason = config.ReasonList.Where(reasonReport => !reasonReport.HideUser).ToList()[ReasonIndex];
	        PlayerInformations[TargetID].ReasonHistory.Add(Reason.Title);

	        GetPlayerCheckServerRCC(TargetID);
	        GetPlayerCheckServerOzProtect(TargetID);
	        
	        BasePlayer Target = BasePlayer.FindByID(TargetID);
	        if (Target != null)
		        SendChat(GetLang("FUNCIONAL_SEND_REPORT_SUCCESS", Sender.UserIDString, Target.displayName), Sender);
		        
	        Configuration.NotifyDiscord.Webhooks.TemplatesNotify TemplateDiscord = config.NotifyDiscordSettings.WebhooksList.NotifySendReport;
			
	        if (!String.IsNullOrWhiteSpace(TemplateDiscord.WebhookNotify))
	        {
		        List<Fields> fields = DT_PlayerSendReport(Sender, TargetID, LanguageEn ? Reason.Title.LanguageEN : Reason.Title.LanguageRU);
		        SendDiscord(TemplateDiscord.WebhookNotify, fields, GetAuthorDiscord(TemplateDiscord), TemplateDiscord.Color);
	        }
	        
	        SendVK(VKT_PlayerSendReport(Sender, TargetID, LanguageEn ? Reason.Title.LanguageEN : Reason.Title.LanguageRU));

	        
	        AddCooldown(Sender.userID, TargetID);

	        if (PlayerInformations[TargetID].Reports >= config.ReportContollerModerationSettings.ReportCountTrigger && Target != null)
	        {
		        foreach (BasePlayer mList in BasePlayer.activePlayerList.Where(m => permission.UserHasPermission(m.UserIDString, ModeratorPermissions)))
			        AlertModerator(mList, Target.displayName, PlayerInformations[TargetID].Reports);
		        
		        AlertMaxReportDiscord(Target.displayName, PlayerInformations[TargetID].Reports, TargetID);
	        }
	        
	        Interface.Call("OnSendedReport", Sender, TargetID, LanguageEn ? Reason.Title.LanguageEN : Reason.Title.LanguageRU);
        }

		
		
		private void DrawUI_LeftMenu(BasePlayer player, Boolean IsModerator = false)
		{
			String Interface = InterfaceBuilder.GetInterface($"{InterfaceBuilder.UI_LAYER}_LEFT_MENU");
			if (Interface == null) return;

			CuiHelper.DestroyUi(player, InterfaceBuilder.UI_REPORT_LEFT_PANEL);
			CuiHelper.AddUi(player, Interface);

			DrawUI_LeftMenu_Button(player, GetLang("TITLE_LEFT_MENU_BUTTON_REPORTS", player.UserIDString),
				ImageUi.GetImage(config.ImagesSettings.LeftBlockSettings.ReportIcon),
				IsModerator ? config.ColorsSettings.AdditionalColorElements : "0 0 0 0", "-70.33 -34", "55 0",
				IsModerator ? $"report.panel select.type.mod {true}" : "");

			if (_.IsModerator(player))
				DrawUI_LeftMenu_Button(player, GetLang("TITLE_LEFT_MENU_BUTTON_MODERATION", player.UserIDString),
					ImageUi.GetImage(config.ImagesSettings.LeftBlockSettings.ModerationIcon),
					IsModerator ? "0 0 0 0" : config.ColorsSettings.AdditionalColorElements, "-70.33 -71.26",
					"55 -37.26", IsModerator ? "" : $"report.panel select.type.mod {false}");
		}

		public Boolean IsFake(UInt64 userID)
		{
			if (!IQFakeActive || !config.ReferenceSettings.IQFakeActiveUse) return false;
			return (Boolean)IQFakeActive?.Call("IsFake", userID);
		}

        public class NpcSound
        {
	        [JsonConverter(typeof(SoundFileConverter))]
	        public List<byte[]> Data = new List<byte[]>();
        }
        private List<byte[]> FromSaveData(byte[] bytes)
        {
	        List<int> dataSize = new List<int>();
	        List<byte[]> dataBytes = new List<byte[]>();

	        int offset = 0;
	        while (true)
	        {
		        dataSize.Add(BitConverter.ToInt32(bytes, offset));
		        offset += 4;

		        int sum = dataSize.Sum();
		        if (sum == bytes.Length - offset)
		        {
			        break;
		        }

		        if (sum > bytes.Length - offset)
		        {
			        throw new ArgumentOutOfRangeException(nameof(dataSize),
				        $"Voice Data is outside the saved range {dataSize.Sum()} > {bytes.Length - offset}");
		        }
	        }

	        foreach (int size in dataSize)
	        {
		        dataBytes.Add(bytes.Skip(offset).Take(size).ToArray());
		        offset += size;
	        }

	        return dataBytes;
        }

        private void Request(String url, String payload, Action<Int32> callback = null)
        {
            Dictionary<String, String> header = new Dictionary<String, String>();
            header.Add("Content-Type", "application/json");
            webrequest.Enqueue(url, payload, (code, response) =>
            {
                if (code != 200 && code != 204)
                {
                    if (response != null)
                    {
                        try
                        {
                            JObject json = JObject.Parse(response);
                            if (code == 429)
                            {
                                Single seconds = Single.Parse(Math.Ceiling((Double)(Int32)json["retry_after"] / 1000).ToString());
                            }
                            else
                            {
                                PrintWarning($" Discord rejected that payload! Responded with \"{json["message"].ToString()}\" Code: {code}");
                            }
                        }
                        catch
                        {
                            PrintWarning($"Failed to get a valid response from discord! Error: \"{response}\" Code: {code}");
                        }
                    }
                    else
                    {
                        PrintWarning($"Discord didn't respond (down?) Code: {code}");
                    }
                }
                try
                {
                    callback?.Invoke(code);
                }
                catch (Exception ex) { }
		   		 		  						  	   		  	   		  	 				   		 		  				
            }, this, RequestMethod.POST, header, timeout: 10F);
        }

        
        
        private void SendPlayerDiscord(BasePlayer player, String Discord)
        {
	        if (player == null) return;
	        if (!PlayerChecks.ContainsKey(player.userID)) return;
	        
	        if (Discord == null)
	        {
		        SendChat(GetLang("FUNCIONAL_SEND_DISCORD_NULL_DS", player.UserIDString), player);
		        return;
	        }

	        if (String.IsNullOrWhiteSpace(Discord))
	        {
		        SendChat(GetLang("FUNCIONAL_SEND_DISCORD_NULL_DS", player.UserIDString), player);
		        return;
	        }
	        Regex regex = new Regex(@"^(?!(here|everyone))^(?!.*(discord|```))(?:[\w #\.]{2,32#}\d{4}|\@[\w\.]{1,32}|[\w\.]{1,32})$");

	        if (!regex.IsMatch(Discord))
	        {
		        SendChat(GetLang("FUNCIONAL_SEND_DISCORD_NO_REGEX_DS", player.UserIDString), player);
		        return;
	        }

	        PlayerChecks[player.userID].DiscordTarget = Discord;
	        
	        SendChat(GetLang("FUNCIONAL_SEND_DISCORD_SUCCESS", player.UserIDString, Discord), player);
	        
	        BasePlayer Moderator = BasePlayer.FindByID(PlayerChecks[player.userID].ModeratorID);
	        if (Moderator != null)
	        {
		        DrawUI_Moderator_Checked_Menu_Discord(Moderator, Discord);
		        SendChat(GetLang("FUNCIONAL_SEND_DISCORD_SUCCESS_ALERT_MODERATOR", Moderator.UserIDString, Discord), Moderator);
	        }
	        else Puts(LanguageEn ? $"The player sent you his Discord. Contact : {Discord}" : $"Игрок прислал вам свой Discord. Контакт : {Discord}");

	        Configuration.NotifyDiscord.Webhooks.TemplatesNotify TemplateDiscord = config.NotifyDiscordSettings.WebhooksList.NotifyContacts;
	        if (!String.IsNullOrWhiteSpace(TemplateDiscord.WebhookNotify))
	        {
		        List<Fields> fields = DT_PlayerSendContact(player, Discord);
		        SendDiscord(TemplateDiscord.WebhookNotify, fields, GetAuthorDiscord(TemplateDiscord), TemplateDiscord.Color);
	        }

	        SendVK(VKT_PlayerSendContact(player, Discord));

	        Interface.Call("OnSendedContacts", player, Discord);
        }
		
		private void DrawUI_PlayerPanel(BasePlayer player, Int32 Page = 0, Boolean IsModerator = false, String SearchName = "")
        {
            String Interface = InterfaceBuilder.GetInterface($"{InterfaceBuilder.UI_LAYER}_PANEL_PLAYERS");
            if (Interface == null) return;
		   		 		  						  	   		  	   		  	 				   		 		  				
            Interface = Interface.Replace("%TITLE_PLAYER_LIST%", GetLang("TITLE_PLAYER_LIST", player.UserIDString));

            CuiHelper.DestroyUi(player, InterfaceBuilder.UI_REPORT_PLAYER_PANEL);
            CuiHelper.AddUi(player, Interface);

            DrawUI_PageController(player, Page, IsModerator, SearchName);
        }

		protected override void LoadConfig()
		{
			base.LoadConfig();
			try
			{
				config = Config.ReadObject<Configuration>();
				if (config == null) LoadDefaultConfig();
		   		 		  						  	   		  	   		  	 				   		 		  				
				if (config.NotifyVKSettings.VKTokenGroup == null)
					config.NotifyVKSettings.VKTokenGroup = "";
				
				if (config.NotifyVKSettings.VKChatID == null)
					config.NotifyVKSettings.VKChatID = "";
			}
			catch
			{
				PrintWarning(LanguageEn ? $"Error #58 reading configuration'oxide/config/{Name}', create a new configuration!" : $"Ошибка #58 чтения конфигурации 'oxide/config/{Name}', создаём новую конфигурацию!!");
				LoadDefaultConfig();
			}
		   		 		  						  	   		  	   		  	 				   		 		  				
			NextTick(SaveConfig);
		}
        
        private readonly Dictionary<UInt64, ProcessCheckRepository> PlayerChecks = new Dictionary<UInt64, ProcessCheckRepository>();

		
		
		
		private Dictionary<UInt64, LocalRepositoryRCC> RCC_LocalRepository = new Dictionary<UInt64, LocalRepositoryRCC>();
		
		private List<String> GetServersBansOzProtect(UInt64 TargetID)
		{
			if (String.IsNullOrWhiteSpace(config.ReferenceSettings.OzProtectSettings.OzProtectKey)) return null;
			return !OzProtect_LocalRepository.ContainsKey(TargetID) ? new List<String>() : OzProtect_LocalRepository[TargetID].LastBansServers;
		}

		private void DrawUI_TemplatePlayer(BasePlayer player, Int32 X, Int32 Y, String NickName, String UserID)
        {
	        String Interface = InterfaceBuilder.GetInterface($"{InterfaceBuilder.UI_LAYER}_TEMPLATE_PLAYER");
            if (Interface == null) return;

            Interface = Interface.Replace("%STEAMID%",GetImage(UserID));
            Interface = Interface.Replace("%NICK%", NickName.Length > 7 ? NickName.Substring(0, 7).ToUpper() + ".." : NickName.ToUpper());
            Interface = Interface.Replace("%OFFSET_MIN%", $"{-219.21 + (X * 160)} {174.14 - (Y * 50)}");//?cache=26850
            Interface = Interface.Replace("%OFFSET_MAX%", $"{-89.97 + (X * 160)} {210.14 - (Y * 50)}");
            Interface = Interface.Replace("%COMMAND%", $"report.panel select.player {UserID} false");
            
            Interface = Interface.Replace("%TITLE_PLAYER_NICK_NAME%", GetLang("TITLE_PLAYER_NICK_NAME", player.UserIDString));

            CuiHelper.AddUi(player, Interface);
        }	
        private const String HideMenuPermissions = "iqreportsystem.hidemenu";
        private class SoundFileConverter : JsonConverter
        {
	        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
	        {
	        }
		   		 		  						  	   		  		 			  			 		   					  		  
	        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
	        {
		        JToken value = JToken.Load(reader);
		        return _.FromSaveData(Compression.Uncompress(Convert.FromBase64String(value.ToString())));
	        }

	        public override bool CanConvert(Type objectType)
	        {
		        return typeof(List<Byte>) == objectType;
	        }
        }
        
                private IEnumerator StartAfkCheck(BasePlayer Target, BasePlayer Moderator, Boolean IsConsole = false, Boolean SkipAFK = false)
        {
	        UInt64 TargetID = Target.userID;
	        if (!IsValidStartChecked(Target, Moderator, IsConsole)) yield break;

	        if (!PlayerChecks.ContainsKey(TargetID))
	        {
				PlayerChecks.Add(Target.userID, new ProcessCheckRepository
				{
					DiscordTarget = String.Empty,
					DisplayName = Target.displayName,
					ModeratorID = !IsConsole ? Moderator.userID : 0
				});    
	        }
	        else
	        {
		        PlayerChecks[TargetID].DisplayName = Target.displayName;
		        PlayerChecks[TargetID].ModeratorID = !IsConsole ? Moderator.userID : 0;
		        PlayerChecks[TargetID].DiscordTarget = String.Empty;
	        }

	        if (!SkipAFK)
	        {
		        if (!IsConsole && Moderator != null)
			        SendChat(GetLang("FUNCIONAL_MESSAGE_CHECK_AFK_STARTING", Moderator.UserIDString), Moderator);
		        else Puts(LanguageEn ? "We start checking the player on AFK" : "Начинаем проверку игрока на AFK");

		        Int32 NoAFK_Amount = 0;
		        for (Int32 TryCheck = 1; TryCheck < 6; TryCheck++)
		        {
			        if (Target == null)
			        {
				        if (!IsConsole && Moderator != null)
					        SendChat(GetLang("FUNCIONAL_MESSAGE_CHECK_AFK_PLAYER_LEAVE", Moderator.UserIDString), Moderator);
				        else Puts(LanguageEn ? "The player left the server at the stage of checking for AFK. Check canceled, try again later" : "Игрок покинул сервер на стадии проверки на AFK. Проверка отменена, повторите попытку позже");

				        StopCheckedPlayer(TargetID, Moderator, true);
				        yield break;
			        }

			        if (!IsConsole)
				        if (Moderator == null)
				        {
					        PrintWarning(LanguageEn
						        ? $"The moderator who called the player {Target.displayName}({Target.UserIDString}) - left the server, interrupting the check AFK!"
						        : $"Модератор вызвавший игрока {Target.displayName}({Target.UserIDString}) - покинул сервер, прервав проверку на AFK!");
					        StopCheckedPlayer(TargetID, Moderator, true);
					        yield break;
				        }

			        if (Target.IdleTime < 10)
				        NoAFK_Amount++;

			        if (!IsConsole && Moderator != null)
				        SendChat(GetLang("FUNCIONAL_MESSAGE_CHECK_AFK_TRY", Moderator.UserIDString, TryCheck), Moderator);
			        else Puts(LanguageEn ? $"Checking a player for AFK. Attemp : {TryCheck}" : $"Проверяем игрока на AFK. Попытка : {TryCheck}"); 
			        yield return CoroutineEx.waitForSeconds(5f);
		        }

		        if (NoAFK_Amount <= 3)
		        {
			        if (!IsConsole && Moderator != null)
				        SendChat(GetLang("FUNCIONAL_MESSAGE_CHECK_AFK_PLAYER_AFK", Moderator.UserIDString), Moderator);
			        else Puts(LanguageEn ? $"Check has not been started. The player is in AFK, please try again later" : "Проверка не была запущена. Игрок находится в AFK, пожалуйста повторите попытку позже");
			        
			        StopCheckedPlayer(TargetID, Moderator, true);
			        yield break;
		        }
	        }

	        StartCheckedPlayer(Target, Moderator, IsConsole);
        }
        
                
        private void SendDiscord(String Webhook, List<Fields> fields, Authors Authors, Int32 Color)
        {
	        if (Webhook == null || String.IsNullOrWhiteSpace(Webhook)) return;
	        FancyMessage newMessage = new FancyMessage(null, false, new FancyMessage.Embeds[1] { new FancyMessage.Embeds(null, Color, fields, Authors) });

	        Request($"{Webhook}", newMessage.toJSON());
        }
		
		
		
		private void ShowPlayersList(BasePlayer player, List<BasePlayer> playersList, List<FakePlayer> fakePlayers, Boolean IsModerator = false)
		{
			Int32 X = 0, Y = 0;
			if (fakePlayers != null)
			{
				foreach (FakePlayer fList in fakePlayers)
				{
					DrawUI_TemplatePlayer(player, X, Y, fList.DisplayName, fList.UserID.ToString());

					X++;
					if (X != 3) continue;
					X = 0;
					Y++;
				}
			}
			else
			{
				foreach (BasePlayer pList in playersList)
				{
					if (IsModerator)
					{
						DrawUI_TemplatePlayer_Moderator(player, Y, pList.UserIDString, CorrectedClanName(pList));
						Y++;
					}
					else
					{
						DrawUI_TemplatePlayer(player, X, Y, CorrectedClanName(pList), pList.UserIDString);
						
						X++;
						if (X != 3) continue;
						X = 0;
						Y++;
					}
				}
			}
		}
        
        
        
        
        
        private String VKT_ChangeStatus(Boolean IsModerator, String PlayerName, String UserID, String StatusConnection)
        {
	        String Message = String.Empty;
	        if (IsModerator)
		        Message = LanguageEn ? $"▣ CONNECTION STATUS ▣\nInformation about the moderator:\n• Nickname: {PlayerName}\n• Steam64ID: {UserID} (https://steamcommunity.com/profiles/{UserID})\n• Status: {StatusConnection}" : $"▣ СТАТУС ПОДКЛЮЧЕНИЯ ▣\nИнформация о модераторе :\n• Ник : {PlayerName}\n• Steam64ID : {UserID} (https://steamcommunity.com/profiles/{UserID})\n• Статус : {StatusConnection}";
	        else Message = LanguageEn ? $"▣ CONNECTION STATUS ▣\nInformation about the suspect:\n• Nickname: {PlayerName}\n• Steam64ID: {UserID} (https://steamcommunity.com/profiles/{UserID})\n• Status: {StatusConnection}" : $"▣ СТАТУС ПОДКЛЮЧЕНИЯ ▣\nИнформация о подозреваемом :\n• Ник : {PlayerName}\n• Steam64ID : {UserID} (https://steamcommunity.com/profiles/{UserID})\n• Статус : {StatusConnection}";

	        return Message;
        }

		private void DrawUI_ModeratorStatistics(BasePlayer Moderator)
		{
			if (!ModeratorInformations.ContainsKey(Moderator.userID))
				ModeratorInformations.Add(Moderator.userID, new ModeratorInformation());
			
			String Interface = InterfaceBuilder.GetInterface($"{InterfaceBuilder.UI_LAYER}_PROFILE_MODERATION_INFO_PANEL");
			if (Interface == null) return;

			Interface = Interface.Replace("%TITLE_PROFILE_MODERATOR_STATISTICS_TITLE%",
				GetLang("TITLE_PROFILE_MODERATOR_STATISTICS_TITLE", Moderator.UserIDString));
			Interface = Interface.Replace("%TITLE_PROFILE_MODERATOR_STATISTICS_TITLE_QUALITY_ASSESSMENT%",
				GetLang("TITLE_PROFILE_MODERATOR_STATISTICS_TITLE_QUALITY_ASSESSMENT", Moderator.UserIDString));
			
			CuiHelper.DestroyUi(Moderator, InterfaceBuilder.UI_REPORT_MODERATOR_STATISTICS);
			CuiHelper.AddUi(Moderator, Interface);

			ModeratorInformation ModeratorInformation = ModeratorInformations[Moderator.userID];
			Int32 AllScoreModerator = ModeratorInformation.GetAverageRaiting();
			
			DrawUI_ModeratorStitistics_Banner(Moderator, "-96.94 -98", "91.72 -44",
				GetLang("TITLE_PROFILE_MODERATOR_STATISTICS_TITLE_CHECKED", Moderator.UserIDString), $"{ModeratorInformation.AmountChecked}",
				GetLang("TITLE_PROFILE_MODERATOR_STATISTICS_DESCRIPTION_BANS_AND_CHECHKED", Moderator.UserIDString));

			DrawUI_ModeratorStitistics_Banner(Moderator, "-96.94 -166.73", "91.72 -112",
				GetLang("TITLE_PROFILE_MODERATOR_STATISTICS_TITLE_BANS", Moderator.UserIDString), $"{ModeratorInformation.AmountBans}",
				GetLang("TITLE_PROFILE_MODERATOR_STATISTICS_DESCRIPTION_BANS_AND_CHECHKED", Moderator.UserIDString));

			DrawUI_ModeratorStitistics_Banner(Moderator, "-96.94 -273.66", "91.72 -219.66",
				GetLang("TITLE_PROFILE_MODERATOR_STATISTICS_TITLE_ALLSCORE", Moderator.UserIDString), $"{AllScoreModerator}",
				CountRaiting: AllScoreModerator);
			
			DrawUI_ModeratorStitistics_Banner(Moderator, "-97 -342.53", "91.66 -288.53",
				GetLang("TITLE_PROFILE_MODERATOR_STATISTICS_TITLE_ONE_ACHIVE", Moderator.UserIDString), $"{ModeratorInformation.GetAverageRaitingAchive(ModeratorInformation.OneScore)}",
				GetLang("TITLE_PROFILE_MODERATOR_STATISTICS_DESCRIPTION_ONE_ACHIVE", Moderator.UserIDString));

			DrawUI_ModeratorStitistics_Banner(Moderator, "-97 -411.53", "91.66 -357.53",
				GetLang("TITLE_PROFILE_MODERATOR_STATISTICS_TITLE_TWO_ACHIVE", Moderator.UserIDString),  $"{ModeratorInformation.GetAverageRaitingAchive(ModeratorInformation.TwoScore)}",
				GetLang("TITLE_PROFILE_MODERATOR_STATISTICS_DESCRIPTION_TWO_ACHIVE", Moderator.UserIDString));

			DrawUI_ModeratorStitistics_Banner(Moderator, "-96.94 -479.66", "91.72 -425.66",
				GetLang("TITLE_PROFILE_MODERATOR_STATISTICS_TITLE_THREE_ACHIVE", Moderator.UserIDString),  $"{ModeratorInformation.GetAverageRaitingAchive(ModeratorInformation.ThreeScore)}",
				GetLang("TITLE_PROFILE_MODERATOR_STATISTICS_DESCRIPTION_THREE_ACHIVE", Moderator.UserIDString));
		}

		
				
		private void DrawUI_Moderator_Checked_Menu(BasePlayer moderator, UInt64 TargetID)
		{
			String Interface = InterfaceBuilder.GetInterface($"{InterfaceBuilder.UI_LAYER}_MODERATOR_MENU_CHECKED");
			if (Interface == null) return;
			
			Interface = Interface.Replace("%COMMAND_STOP_CHECKED%", $"report.panel stop.check.player {TargetID}");
			Interface = Interface.Replace("%TITLE_PROFILE_MODERATOR_CHECKED_MENU_TITLES%", GetLang("TITLE_PROFILE_MODERATOR_CHECKED_MENU_TITLES", moderator.UserIDString));
			Interface = Interface.Replace("%TITLE_PROFILE_MODERATOR_CHECKED_MENU_TITLE_BUTTON_STOP%", GetLang("TITLE_PROFILE_MODERATOR_CHECKED_MENU_TITLE_BUTTON_STOP", moderator.UserIDString));

			CuiHelper.AddUi(moderator, Interface);

			DrawUI_Moderator_Button(moderator, $"report.panel check.show.verdicts {TargetID}");
			DrawUI_Moderator_Checked_Menu_Status(moderator, GetLang("TITLE_PROFILE_MODERATOR_CHECKED_MENU_TITLE_STATUS_DEFAULT", moderator.UserIDString));
			DrawUI_Moderator_Checked_Menu_Discord(moderator, GetLang("TITLE_PROFILE_MODERATOR_CHECKED_MENU_TITLE_DISCORD_EMPTY", moderator.UserIDString));
		}		

				
		private void DrawUI_Reason_Raiting_Or_Moderator_Menu(BasePlayer moderator, String Text, String ParentUI, String NameLayer, String OffsetMin, String OffsetMax, String Command)
		{
			String Interface = InterfaceBuilder.GetInterface($"{InterfaceBuilder.UI_LAYER}_REASON_MENU_LABEL");
			if (Interface == null) return;

			Interface = Interface.Replace("%PARENT_UI%", ParentUI);
			Interface = Interface.Replace("%REASON_NAME%", NameLayer);
			Interface = Interface.Replace("%OFFSET_MIN%", OffsetMin);
			Interface = Interface.Replace("%OFFSET_MAX%", OffsetMax);
			Interface = Interface.Replace("%TEXT_TITLE%", Text);
			Interface = Interface.Replace("%COMMAND_REASON%", Command);

			CuiHelper.AddUi(moderator, Interface);
		}
		private void DrawUI_HeaderUI_Search(BasePlayer player, Boolean IsModerator = false)
        {
	        String Interface = InterfaceBuilder.GetInterface($"{InterfaceBuilder.UI_LAYER}_SEARCH_HEADER");
            if (Interface == null) return;

            Interface = Interface.Replace("%ISMODERATOR%", $"{IsModerator}");
            Interface = Interface.Replace("%TITLE_PLAYER_HEADER_TITLE_SEARCH_PLAYER%", GetLang("TITLE_PLAYER_HEADER_TITLE_SEARCH_PLAYER", player.UserIDString));

            CuiHelper.DestroyUi(player, "InputPanelSearch" + ".Input");
            CuiHelper.AddUi(player, Interface);
        }	
        
        private void StartCheckedPlayer(BasePlayer Target, BasePlayer Moderator, Boolean IsConsole = false)
        {
	        if (!IsConsole)
	        {
		        if (Moderator == null)
		        {
			        PrintWarning(LanguageEn
				        ? $"The moderator who called the player {Target.displayName}({Target.UserIDString}) - left the server, interrupting start checking!"
				        : $"Модератор вызвавший игрока {Target.displayName}({Target.UserIDString}) - покинул сервер, прервав запуск проверки!");
			        StopCheckedPlayer(Target.userID, Moderator);
			        return;
		        }

		        DrawUI_Moderator_Checked_Menu(Moderator, Target.userID);
	        }
	        
	        PlayerChecks[Target.userID].DisplayName = Target.displayName;

	        UInt64 ModratorID = !IsConsole ? Moderator.userID : 0;
	        StartCheckRCC(Target.userID, ModratorID);
	        StartCheckOzProtect(Target.userID, ModratorID);
	        DrawUI_Player_Alert(Target);
	        
	        if(config.CheckControllerSettings.UseDemo)
		        Target.StartDemoRecording();
		   		 		  						  	   		  	   		  	 				   		 		  				
	        StopDamageAdd(Target);
	        
	        if (config.NotifyChatSettings.UseNotifyCheck)
	        {
		        if (!IsConsole && Moderator != null)
		        {
			        foreach (BasePlayer player in BasePlayer.activePlayerList)
				        SendChat(GetLang("NOTIFY_PLAYERS_START_CHECK_MODERATOR", player.UserIDString, Moderator.displayName, PlayerChecks[Target.userID].DisplayName), player);
		        }
		        else
		        {
			        foreach (BasePlayer player in BasePlayer.activePlayerList)
				        SendChat(GetLang("NOTIFY_PLAYERS_START_CHECK_NOT_MODERATOR", player.UserIDString, Target.displayName), player);
		        }
	        }

	        Configuration.NotifyDiscord.Webhooks.TemplatesNotify TemplateDiscord = config.NotifyDiscordSettings.WebhooksList.NotifyStartCheck;
			
	        if (!String.IsNullOrWhiteSpace(TemplateDiscord.WebhookNotify))
	        {
		        List<Fields> fields = DT_StartCheck(Target, Moderator, IsConsole);
		        SendDiscord(TemplateDiscord.WebhookNotify, fields, GetAuthorDiscord(TemplateDiscord), TemplateDiscord.Color);
	        }

	        SendVK(VKT_StartCheck(Target, Moderator, IsConsole));
	        
	        if(!IsConsole && Moderator != null)
		        SendChat(GetLang("FUNCIONAL_MESSAGE_CHECK_START", Moderator.UserIDString), Moderator);
	        else Puts(LanguageEn ? "The check is started, the player is notified!" : "Проверка запущена, игрок уведомлен!");

	        if (config.ReportSendControllerSettings.NoRepeatReport)
	        {
		        PlayerRepository repository = PlayerRepositories.FirstOrDefault(x => x.Value.IsRepeatReported(Target.userID)).Value;
		        if (repository != null)
			        repository.ReportedList.Remove(Target.userID);
	        }

	        Interface.Call("OnStartedChecked", Target, Moderator, IsConsole);
        }

        public class Authors
        {
            public String name { get; set; }
            public String url { get; set; }
            public String icon_url { get; set; }
            public String proxy_icon_url { get; set; }
            public Authors(String name, String url, String icon_url, String proxy_icon_url)
            {
                this.name = name;
                this.url = url;
                this.icon_url = icon_url;
                this.proxy_icon_url = proxy_icon_url;
            }
        }

		private void Unload()
		{
			WriteData();
			
			InterfaceBuilder.DestroyAll();
			ImageUi.Unload();

			if (config.CheckControllerSettings.UseSoundAlert)
			{
				if (RoutineSounds != null && RoutineSounds.Count != 0)
				{
					foreach (KeyValuePair<BasePlayer, Coroutine> routineSound in RoutineSounds.Where(x => x.Value != null))
						ServerMgr.Instance.StopCoroutine(routineSound.Value);
				}

				SpeakerEntityMgr.Shutdown();
			}
		   		 		  						  	   		  	   		  	 				   		 		  				
			if (AfkCheckRoutine != null && AfkCheckRoutine.Count != 0)
			{
				foreach (KeyValuePair<BasePlayer, Coroutine> coroutineList in AfkCheckRoutine.Where(x => x.Value != null))
					ServerMgr.Instance.StopCoroutine(coroutineList.Value);
				
				AfkCheckRoutine.Clear();
				AfkCheckRoutine = null;
			}

			if (PlayerRepositories != null && PlayerRepositories.Count != 0)
			{
				PlayerRepositories.Clear();
				PlayerRepositories = null;
			}
		   		 		  						  	   		  	   		  	 				   		 		  				
			if (TimerWaitChecked != null && TimerWaitChecked.Count != 0)
			{
				foreach (Timer timer in TimerWaitChecked.Values.Where(t => !t.Destroyed))
					timer.Destroy();
				
				TimerWaitChecked.Clear();
				TimerWaitChecked = null;
			}

			if (config.CheckControllerSettings.StopCheckLeavePlayer)
			{
				if (TimerWaitPlayer != null && TimerWaitPlayer.Count != 0)
				{
					foreach (Timer timer in TimerWaitPlayer.Values.Where(t => !t.Destroyed))
						timer.Destroy();

					TimerWaitPlayer.Clear();
					TimerWaitPlayer = null;
				}
			}
		   		 		  						  	   		  	   		  	 				   		 		  				
			_interface = null;
			_ = null;
		}
		   		 		  						  	   		  	   		  	 				   		 		  				

				
		object OnPlayerChat(BasePlayer player, String message, Chat.ChatChannel channel)
		{
			if (!PlayerChecks.ContainsKey(player.userID)) return null;

			if (config.CheckControllerSettings.TrackChat)
			{
				BasePlayer Moderator = BasePlayer.FindByID(PlayerChecks[player.userID].ModeratorID);
				if (Moderator != null) 
					SendChat(GetLang("FUNCIONAL_MESSAGE_CHECK_AFK_PLAYER_SEND_CHAT", Moderator.UserIDString, message), Moderator);
			}

			player.ResetInputIdleTime();
			return null;
		}

		public class OzResult
		{
			public string status { get; set; }
			public List<OzResponse> response { get; set; }
		}

				
		
		private void DrawUI_PoopUp_Moderator(BasePlayer player, BasePlayer Target)
		{
			String Interface = InterfaceBuilder.GetInterface($"{InterfaceBuilder.UI_LAYER}_POOPUP_MODERATOR");
			if (Interface == null) return;
			
			PlayerInformation InformationTarget = PlayerInformations[Target.userID];
			String LastModerator = String.IsNullOrWhiteSpace(InformationTarget.LastModerator) ? GetLang("TITLE_POOPUP_MODERATION_NO_CHECKED", player.UserIDString) : InformationTarget.LastModerator;
			String AmountCheck = InformationTarget.AmountChecked == 0 ? GetLang("TITLE_POOPUP_MODERATION_NO_CHECKED", player.UserIDString) : InformationTarget.AmountChecked.ToString();

			Interface = Interface.Replace("%TITLE_POOPUP_MODERATION_REPORTS_TITLE%", GetLang("TITLE_POOPUP_MODERATION_REPORTS_TITLE", player.UserIDString));
			Interface = Interface.Replace("%TITLE_POOPUP_MODERATION_AMOUNT_CHECK_TITLE%", GetLang("TITLE_POOPUP_MODERATION_AMOUNT_CHECK_TITLE", player.UserIDString));
			Interface = Interface.Replace("%TITLE_POOPUP_MODERATION_LAST_CHECK_TITLE%", GetLang("TITLE_POOPUP_MODERATION_LAST_CHECK_TITLE", player.UserIDString));
			Interface = Interface.Replace("%TITLE_PLAYER_STEAMID%", GetLang("TITLE_PLAYER_STEAMID", player.UserIDString));
			Interface = Interface.Replace("%TITLE_PLAYER_NICK_NAME%", GetLang("TITLE_PLAYER_NICK_NAME", player.UserIDString));
			Interface = Interface.Replace("%TITLE_POOPUP_MODERATION_INFO_BUTTON_START_CHECK%", GetLang("TITLE_POOPUP_MODERATION_INFO_BUTTON_START_CHECK", player.UserIDString));
			
			Interface = Interface.Replace("%PLAYER_NAME%", Target.displayName.Length > 7 ? Target.displayName.Substring(0, 7).ToUpper() + ".." : Target.displayName.ToUpper());
			Interface = Interface.Replace("%PLAYER_USERID%", Target.UserIDString);
			Interface = Interface.Replace("%AMOUNT_REPORTS%", InformationTarget.Reports.ToString());
			Interface = Interface.Replace("%AMOUNT_CHECK%", AmountCheck);
			Interface = Interface.Replace("%LAST_MODER_CHECK_NAME%", LastModerator);
			Interface = Interface.Replace("%AVATAR%", GetImage(Target.UserIDString));
			Interface = Interface.Replace("%COMMAND_START%",  $"report.panel start.check.player {Target.UserIDString}" );

			CuiHelper.AddUi(player, Interface);
	
			///История жалоб
			DrawUI_PoopUp_Moderator_Panel_Info(player, Target, GetLang("TITLE_POOPUP_MODERATION_HISTORY_REPORTS_TITLE", player.UserIDString), InformationTarget.ReasonHistory.Take(6).ToList(), "-232.80 -141.2", "-102.14 18.8");
		   		 		  						  	   		  	   		  	 				   		 		  				
			if (!String.IsNullOrEmpty(config.ReferenceSettings.RCCSettings.RCCKey))
			{
				///История проверок на серверах [RCC]
				DrawUI_PoopUp_Moderator_Panel_Info(player, Target,
					GetLang("TITLE_POOPUP_MODERATION_INFO_CHECK_SERVERS_RCC", player.UserIDString), null,
					"-65.33 -141.2", "65.33 18.8", GetServersCheckRCC(Target.userID).Take(6).ToList());
		   		 		  						  	   		  	   		  	 				   		 		  				
				///История банов на серверах [RCC]
				DrawUI_PoopUp_Moderator_Panel_Info(player, Target,
					GetLang("TITLE_POOPUP_MODERATION_INFO_BANS_SERVERS_RCC", player.UserIDString), null,
					"102.83 -141.2", "233.5 18.8", GetServersBansRCC(Target.userID).Take(6).ToList());

				return;
			}

			if (!String.IsNullOrWhiteSpace(config.ReferenceSettings.OzProtectSettings.OzProtectKey))
			{
				///Тиммейты игрока
				DrawUI_PoopUp_Moderator_Panel_Info(player, Target,
					GetLang("TITLE_POOPUP_MODERATION_INFO_TEAMS_NAME_PLAYER", player.UserIDString), null,
					"-65.33 -141.2", "65.33 18.8", GetTeamsNames(Target).Take(6).ToList());

				///История банов на серверах [OzProtect]
				DrawUI_PoopUp_Moderator_Panel_Info(player, Target,
					GetLang("TITLE_POOPUP_MODERATION_INFO_BANS_SERVERS_OZPROTECT", player.UserIDString), null,
					"102.83 -141.2", "233.5 18.8", GetServersBansOzProtect(Target.userID).Take(6).ToList());
				
				return;
			}
			
			///Тиммейты игрока
			DrawUI_PoopUp_Moderator_Panel_Info(player, Target,
				GetLang("TITLE_POOPUP_MODERATION_INFO_TEAMS_NAME_PLAYER", player.UserIDString), null,
				"-65.33 -141.2", "65.33 18.8", GetTeamsNames(Target).Take(6).ToList());
		}
        private class PlayerRepository
	    {

		    public Int32 GetCooldownLeft(UInt64 TargetID)
		    {
			    if (config.ReportSendControllerSettings.CooldownRepeatOrAll)
			    {
				    if (IsReportedPlayer(TargetID))
					    return Convert.ToInt32(ReportedList[TargetID] - CurrentTime);
			    }
			    else return Convert.ToInt32(Cooldown - CurrentTime);

			    return 0;
		    }
		    
		    private Boolean IsReportedPlayer(UInt64 TargetID) => ReportedList.ContainsKey(TargetID);
		    public Boolean IsCooldown(UInt64 TargetID)
		    {
			    if (config.ReportSendControllerSettings.CooldownRepeatOrAll)
			    {
				    if (IsReportedPlayer(TargetID))
					    return ReportedList[TargetID] > CurrentTime;
			    }
			    else return Cooldown > CurrentTime;

			    return false;
		    }
		    public Double Cooldown;

		    public void AddCooldown(UInt64 TargetID)
		    {
			    Int32 TimeCooldown = Convert.ToInt32(config.ReportSendControllerSettings.CooldownReport + CurrentTime);
			    
			    if (config.ReportSendControllerSettings.CooldownRepeatOrAll ||
			        config.ReportSendControllerSettings.NoRepeatReport)
			    {
				    if (IsReportedPlayer(TargetID))
					    ReportedList[TargetID] = TimeCooldown;
				    else ReportedList.Add(TargetID, TimeCooldown);
			    }
			    else Cooldown = TimeCooldown;
		    }
		    public Boolean IsRepeatReported(UInt64 TargetID) => config.ReportSendControllerSettings.NoRepeatReport && IsReportedPlayer(TargetID);
		    public Dictionary<UInt64, Double> ReportedList = new Dictionary<UInt64, Double>();
	    }
        
                [PluginReference] Plugin IQChat, ImageLibrary, IQFakeActive, NoEscape, EventHelper, Battles, Duel, Duelist, ArenaTournament, Friends, Clans, MultiFighting, StopDamageMan;
		private void GetPlayerCheckServerOzProtect(UInt64 TargetID)
		{
			if (String.IsNullOrWhiteSpace(config.ReferenceSettings.OzProtectSettings.OzProtectKey)) return;
			if (OzProtect_LocalRepository.ContainsKey(TargetID)) return;
			
			String API = $"https://api.ozliginus.ru/methods/ozprotect.getbans?steamid={TargetID}&ozprotectid={config.ReferenceSettings.OzProtectSettings.OzProtectKey}";
			try
			{
				webrequest.Enqueue(API, null, (code, response) =>
				{
					OzResult resources = JsonConvert.DeserializeObject<OzResult>(response);
					if (resources.response == null || !resources.status.Equals("success"))
						return;
					
					OzProtect_LocalRepository.Add(TargetID, new LocalRepositoryOzProtect());
					foreach (OzResponse ozResponse in resources.response)
					{
						if (!OzProtect_LocalRepository[TargetID].LastBansServers.Contains(ozResponse.server.name))
							OzProtect_LocalRepository[TargetID].LastBansServers.Add(ozResponse.server.name);
					}

				}, this);
			}
			catch (Exception e)
			{
				PrintError(LanguageEn ? "OzProtect : We couldn't find player information with OzProtect, please check if your key is up to date or if OzProtect is available" : "OzProtect : Мы не смогли найти информацию об игроке с помощью OzProtect, проверьте актуальность вашего ключа или доступность OzProtect");
			}
		}
		
		private void GetPlayerCheckServerRCC(UInt64 TargetID)
		{
			if (String.IsNullOrWhiteSpace(config.ReferenceSettings.RCCSettings.RCCKey)) return;
			if (RCC_LocalRepository.ContainsKey(TargetID)) return;
			
			String API = $"https://rustcheatcheck.ru/panel/api?action=getInfo&key={config.ReferenceSettings.RCCSettings.RCCKey}&player={TargetID}";
			try
			{
				webrequest.Enqueue(API, null, (code, response) =>
				{
					Response resources = JsonConvert.DeserializeObject<Response>(response);
					if (resources.last_check == null)
						return;
		   		 		  						  	   		  	   		  	 				   		 		  				
					RCC_LocalRepository.Add(TargetID, new LocalRepositoryRCC());
					RCC_LocalRepository[TargetID].LastChecksServers.AddRange(resources.last_check.Select(resource => resource.serverName));
					RCC_LocalRepository[TargetID].LastBansServers.AddRange(resources.bans.Select(resource => resource.serverName));
					
				}, this);
			}
			catch (Exception ex)
			{
				PrintError(LanguageEn ? "RCC : We couldn't find player information with RCC, please check if your key is up to date or if RCC is available" : "RCC : Мы не смогли найти информацию об игроке с помощью RCC, проверьте актуальность вашего ключа или доступность RCC");
			}
		}
		   		 		  						  	   		  	   		  	 				   		 		  				
		
		private void BanPlayerOzProtect(UInt64 TargetID, UInt64 ModerID, String ReasonBan)
		{
			if (String.IsNullOrWhiteSpace(config.ReferenceSettings.OzProtectSettings.OzProtectKey)) return;
			String API = $"https://api.ozliginus.ru/methods/ozprotect.useredit?admin={ModerID}&steamid={TargetID}&method=ban&reason={ReasonBan}&ozprotectid={config.ReferenceSettings.OzProtectSettings.OzProtectKey}";
			try
			{
				webrequest.Enqueue(API, null, (code, response) => { }, this);
			}
			catch (Exception e)
			{
				PrintError(LanguageEn ? "OzProtect : We were unable to block the player using OzProtect, please check if your key is up to date or if OzProtect is available" : "OzProtect : Мы не смогли заблокировать игрока с помощью OzProtect, проверьте актуальность вашего ключа или доступность OzProtect");
			}
		}
		
		private void StartCheckOzProtect(UInt64 TargetID, UInt64 ModerID)
		{
			if (ModerID == 0) return;
			if (String.IsNullOrWhiteSpace(config.ReferenceSettings.OzProtectSettings.OzProtectKey)) return;
			String API = $"https://api.ozliginus.ru/methods/ozprotect.call?checking={ModerID}&suspect={TargetID}&ozprotectid={config.ReferenceSettings.OzProtectSettings.OzProtectKey}&minutes=30";

			try
			{
				webrequest.Enqueue(API, null, (code, response) => { }, this);
			}
			catch (Exception e)
			{
				PrintError(LanguageEn ? "OzProtect : We were unable to get the player to check with OzProtect, please check if your key is up to date or if OzProtect is available" : "OzProtect : Мы не смогли вызвать игрока на проверку с помощью OzProtect, проверьте актуальность вашего ключа или доступность OzProtect");
			}
		}

	    private class ProcessCheckRepository
        {
	        public String DiscordTarget;
	        public String DisplayName;

	        public UInt64 ModeratorID;
        }

        private String GetUrlVK(String Message)
        {
	        String ApiKey = config.NotifyVKSettings.VKTokenGroup;
	        Int32 RandomID = Oxide.Core.Random.Range(0, 99999);
	        Int32 PeerID = 2000000000 + Convert.ToInt32(config.NotifyVKSettings.VKChatID);
	        
	        String url = $"https://api.vk.com/method/messages.send?chat_id=1&random_id={RandomID}&peer_id={PeerID}&message={Message}&access_token={ApiKey}&v=5.13";
	        return url;
        }

        
        
        private void StopCheckedPlayer(UInt64 TargetID, BasePlayer Moderator, Boolean AutoStop = false, Boolean IsConsole = false)
        {
	        if (!PlayerChecks.ContainsKey(TargetID)) return;
	        StopDamageRemove(TargetID);
	        
	        BasePlayer Target = BasePlayer.FindByID(TargetID);

	        if (Target != null)
	        {
		        if(config.CheckControllerSettings.UseDemo)
			        Target.StopDemoRecording();
		        
		        if (!AutoStop)
			        DrawUI_Raiting_Menu_Player(Target, PlayerChecks[TargetID].ModeratorID);
		        CuiHelper.DestroyUi(Target, InterfaceBuilder.UI_REPORT_PLAYER_ALERT);
		        
		        if (AfkCheckRoutine.ContainsKey(Target))
		        {
			        if (AfkCheckRoutine[Target] != null)
			        {
				        ServerMgr.Instance.StopCoroutine(AfkCheckRoutine[Target]);
				        AfkCheckRoutine[Target] = null;
			        }

			        AfkCheckRoutine.Remove(Target);
		        }
	        }
	        
	        if (!IsConsole)
	        {
		        if (Moderator != null)
		        {
			        CuiHelper.DestroyUi(Moderator, InterfaceBuilder.UI_REPORT_MODERATOR_MENU_CHECKED);
			        ModeratorInformations[Moderator.userID].AmountChecked++;
			        PlayerInformations[TargetID].LastModerator = Moderator.displayName;
		        }
	        }
	        else Puts(LanguageEn ? "Player checked completed" : "Проверка игрока завершена");
	        
	        if (!AutoStop)
	        {
		        if (config.NotifyChatSettings.UseNotifyStopCheck)
		        {
			        if (Moderator != null || !IsConsole)
			        {
				        foreach (BasePlayer player in BasePlayer.activePlayerList)
					        SendChat(GetLang("NOTIFY_PLAYERS_STOP_CHECK_MODERATOR", player.UserIDString, Moderator.displayName, PlayerChecks[TargetID].DisplayName), player);
			        }
			        else
			        {
				        foreach (BasePlayer player in BasePlayer.activePlayerList)
					        SendChat(GetLang("NOTIFY_PLAYERS_STOP_CHECK_NOT_MODERATOR", player.UserIDString, PlayerChecks[TargetID].DisplayName), player);
			        }
		        }
		        
		        PlayerInformations[TargetID].AmountChecked++;
		        PlayerInformations[TargetID].Reports = 0;
	        }

	        Configuration.NotifyDiscord.Webhooks.TemplatesNotify TemplateDiscord = config.NotifyDiscordSettings.WebhooksList.NotifyStopCheck;
			
	        if (!String.IsNullOrWhiteSpace(TemplateDiscord.WebhookNotify))
	        {
		        List<Fields> fields = DT_StopCheck(TargetID, Moderator, AutoStop, IsConsole);
		        SendDiscord(TemplateDiscord.WebhookNotify, fields, GetAuthorDiscord(TemplateDiscord), TemplateDiscord.Color);
	        }
	        
	        SendVK(VKT_StopCheck(TargetID, Moderator, AutoStop, IsConsole));


	        if (PlayerChecks.ContainsKey(TargetID))
		        PlayerChecks.Remove(TargetID);

	        Interface.Call("OnStoppedChecked", Target, Moderator, AutoStop, IsConsole);
        }
        private void SoundPlay(BasePlayer player)
        {
	        SpeakerEntityMgr.SpeakerEntity speaker = SpeakerEntityMgr.Create(player);
            
	        speaker.SendEntitities();
	        String SoundName = lang.GetLanguage(player.UserIDString).Equals("ru") ? "ALERT_REPORT_RU" : "ALERT_REPORT_EN";
	        
	        Coroutine routine = ServerMgr.Instance.StartCoroutine(SendSounds(player, SoundName, speaker));

	        if (!RoutineSounds.ContainsKey(player))
		        RoutineSounds.Add(player, routine);
	        else
	        {
		        if (RoutineSounds[player] != null)
			        ServerMgr.Instance.StopCoroutine(RoutineSounds[player]);
			        
		        RoutineSounds[player] = routine;
	        }
	        timer.Once(15f, () =>
	        {
		        speaker.Kill();
		        
		        if (RoutineSounds[player] != null)
			        ServerMgr.Instance.StopCoroutine(RoutineSounds[player]);
	        });     
        }

		private Boolean IsCombatBlock(BasePlayer Target)
		{
			if (!NoEscape) return false;
			if (!config.ReferenceSettings.NoEscapeSetting.NoCheckedCombatBlock) return false;
			Boolean IsCombatBlock = (Boolean)NoEscape.CallHook("IsCombatBlocked", Target);

			return IsCombatBlock;
		}
        
        private String VKT_PlayerSendReport(BasePlayer Sender, UInt64 TargetID, String Reason)
        {
	        String Message = LanguageEn ? $"▣ NEW COMPLAINT ▣" +
	                                      $"\nInformation about the sender:\n• Nickname: {Sender.displayName}\n• Steam64ID: {Sender.userID} (https://steamcommunity.com/profiles/{Sender.userID})" +
	                                      $"\nInformation about the suspect:\n• Nickname: {covalence.Players.FindPlayerById(TargetID.ToString()).Name ?? "EMPTY"}\n• Steam64ID: {TargetID} (https://steamcommunity.com/profiles/{TargetID})" +
	                                      $"\nComplaint reason: {Reason}" : $"▣ НОВАЯ ЖАЛОБА ▣" +
	                                                                        $"\nИнформация об отправителе :\n• Ник : {Sender.displayName}\n• Steam64ID : {Sender.userID} (https://steamcommunity.com/profiles/{Sender.userID})" +
	                                                                        $"\nИнформация о подозреваемом :\n• Ник : {covalence.Players.FindPlayerById(TargetID.ToString()).Name ?? "EMPTY"}\n• Steam64ID : {TargetID} (https://steamcommunity.com/profiles/{TargetID})" +
	                                                                        $"\nПричина жалобы : {Reason}";

	        return Message;
        }

		
		
		private Boolean IsFriendSendReport(UInt64 userID, UInt64 targetID) => config.ReferenceSettings.FriendsSetting.SendReportFriend && IsFriends(userID, targetID);

        
                
        private void CheckStatusModerator(BasePlayer Moderator, String ReasonDisconnected = null)
        {
	        if (Moderator == null) return;
	        KeyValuePair<UInt64, ProcessCheckRepository> ModeratorCheckeds = PlayerChecks.FirstOrDefault(m => m.Value.ModeratorID.Equals(Moderator.userID));
	        if (ModeratorCheckeds.Value == null) return;
	        
	        Configuration.NotifyDiscord.Webhooks.TemplatesNotify TemplateDiscord = config.NotifyDiscordSettings.WebhooksList.NotifyStatusPlayerOrModerator;

	        UInt64 TargetID = ModeratorCheckeds.Key;
	        BasePlayer Target = BasePlayer.FindByID(TargetID);

	        if (ReasonDisconnected != null)
	        {
		        if (Target == null)
		        {
			        StopCheckedPlayer(TargetID, Moderator);
			        return;
		        }
		        
		        SendChat(GetLang("FUNCIONAL_CHANGE_STATUS_MODERATOR_DISCONNECTED", Target.UserIDString), Target);
		        
		        Timer WaitModerator = timer.Once(600f, () =>
		        {
			        StopCheckedPlayer(Target.userID, null, true);
			        SendChat(GetLang("FUNCIONAL_CHANGE_STATUS_MODERATOR_DISCONNECTED_FULL_LEAVE", Target.UserIDString), Target);
		        });

		        if (!TimerWaitChecked.ContainsKey(Target.userID))
			        TimerWaitChecked.Add(Target.userID, WaitModerator);
		        else TimerWaitChecked[Target.userID] = WaitModerator;
		        
		        if (!String.IsNullOrWhiteSpace(TemplateDiscord.WebhookNotify))
		        {
			        List<Fields> fields = DT_ChangeStatus(true, Moderator.displayName, Moderator.UserIDString, ReasonDisconnected);
			        SendDiscord(TemplateDiscord.WebhookNotify, fields, GetAuthorDiscord(TemplateDiscord), TemplateDiscord.Color);
		        }
		        
		        SendVK(VKT_ChangeStatus(true, Moderator.displayName, Moderator.UserIDString, ReasonDisconnected));
		        return;
	        }
		   		 		  						  	   		  	   		  	 				   		 		  				
	        if (!TimerWaitChecked.ContainsKey(TargetID)) return;
	        if (TimerWaitChecked[TargetID].Destroyed) return;
	        
	        TimerWaitChecked[TargetID].Destroy();
	        TimerWaitChecked.Remove(TargetID);

	        DrawUI_Moderator_Checked_Menu(Moderator, TargetID);

	        String TargetStatus = GetLang("TITLE_PROFILE_MODERATOR_CHECKED_MENU_TITLE_STATUS_DEFAULT", Moderator.UserIDString);
	        if (Target == null || !Target.IsConnected)
	        {
		        TargetStatus = "Disconnected";
		        SendChat(GetLang("FUNCIONAL_CHANGE_STATUS_PLAYER_ALERT_MODERATOR", Moderator.UserIDString, TargetStatus), Moderator);
	        }

	        DrawUI_Moderator_Checked_Menu_Status(Moderator, TargetStatus);
	        DrawUI_Moderator_Checked_Menu_Discord(Moderator, String.IsNullOrWhiteSpace(PlayerChecks[TargetID].DiscordTarget) ? GetLang("TITLE_PROFILE_MODERATOR_CHECKED_MENU_TITLE_DISCORD_EMPTY", Moderator.UserIDString) : PlayerChecks[TargetID].DiscordTarget);

	        if (Target != null)
		        SendChat(GetLang("FUNCIONAL_CHANGE_STATUS_MODERATOR_RECONNECTED", Target.UserIDString), Target);
	        
	        if (!String.IsNullOrWhiteSpace(TemplateDiscord.WebhookNotify))
	        {
		        List<Fields> fields = DT_ChangeStatus(true, Moderator.displayName, Moderator.UserIDString, "Online");
		        SendDiscord(TemplateDiscord.WebhookNotify, fields, GetAuthorDiscord(TemplateDiscord), TemplateDiscord.Color);
	        }
	        
	        SendVK(VKT_ChangeStatus(true, Moderator.displayName, Moderator.UserIDString, "Online"));

        }
        
        private String VKT_StopCheck(UInt64 TargetID, BasePlayer Moderator, Boolean AutoStop = false, Boolean IsConsole = false, Configuration.ReasonReport Verdict = null)
        { 
	        String ModeratorName = !IsConsole && Moderator != null ? Moderator.displayName : "Console";
	        String ModeratorID = !IsConsole && Moderator != null ? Moderator.UserIDString : "Console";
	        String Message = String.Empty;

	        if (AutoStop)
	        {
		        Message = LanguageEn
			        ? $"▣ PLAYER CHECK AUTOMATICALLY COMPLETED ▣\nInformation about the moderator:\n• Nickname: {ModeratorName}\n• Steam64ID: {ModeratorID} (https://steamcommunity.com/profiles/{ModeratorID})\nInformation about the suspect:\n• Nickname: {PlayerChecks[TargetID].DisplayName}\n• Steam64ID: {TargetID} (https://steamcommunity.com/profiles/{TargetID})\nResult: The player's reports are not reset"
			        : $"▣ ПРОВЕРКА ИГРОКА ЗАВЕРШЕНА АВТОМАТИЧЕСКИ ▣\nИнформация о модераторе :\n• Ник : {ModeratorName}\n• Steam64ID : {ModeratorID} (https://steamcommunity.com/profiles/{ModeratorID})\nИнформация о подозреваемом :\n• Ник : {PlayerChecks[TargetID].DisplayName}\n• Steam64ID : {TargetID} (https://steamcommunity.com/profiles/{TargetID})\nРезультат : Репорты игрока не сброшены";
	        }
	        else
	        {
		        Message = LanguageEn
			        ? $"▣ PLAYER CHECK COMPLETED ▣\nInformation about the moderator:\n• Nickname: {ModeratorName}\n• Steam64ID: {ModeratorID} (https://steamcommunity.com/profiles/{ModeratorID})\nInformation about the suspect:\n• Nickname: {PlayerChecks[TargetID].DisplayName}\n• Steam64ID: {TargetID} (https://steamcommunity.com/profiles/{TargetID})\nResult: {(Verdict == null ? "No violations found" : Verdict.Title.LanguageEN)}"
			        : $"▣ ПРОВЕРКА ИГРОКА ЗАВЕРШЕНА ▣\nИнформация о модераторе :\n• Ник : {ModeratorName}\n• Steam64ID : {ModeratorID} (https://steamcommunity.com/profiles/{ModeratorID})\nИнформация о подозреваемом :\n• Ник : {PlayerChecks[TargetID].DisplayName}\n• Steam64ID : {TargetID} (https://steamcommunity.com/profiles/{TargetID})\nРезультат : {(Verdict == null ? "Нарушений не выявлено" : Verdict.Title.LanguageRU)}";
	        }

	        return Message;
        }

		public class OzServer
		{
			public string name { get; set; }
			public string ico { get; set; }
			public string overlay { get; set; }
			public string desc { get; set; }
			public string site { get; set; }
			public bool pirate { get; set; }
			public int game { get; set; }
			public string ip { get; set; }
		}
		
		
		public String FindFakeName(ulong userID)
		{
			if (!IQFakeActive || !config.ReferenceSettings.IQFakeActiveUse) return "PLAYER";
			return (string)IQFakeActive?.Call("FindFakeName", userID);
		}
        
        private Authors GetAuthorDiscord(Configuration.NotifyDiscord.Webhooks.TemplatesNotify templatesNotify) => new Authors(templatesNotify.AuthorName, null, templatesNotify.IconURL, null);
		private Boolean IsFriendStartChecked(UInt64 userID, UInt64 targetID) => config.ReferenceSettings.FriendsSetting.StartCheckedFriend && IsFriends(userID, targetID);
		
				private Boolean IsClansSendReport(String userID, String targetID) => config.ReferenceSettings.ClansSetting.SendReportClan && IsClans(userID, targetID);
        
		[ConsoleCommand("report.panel")]
		private void FuncionalCommandReport(ConsoleSystem.Arg arg)
        {
			BasePlayer player = arg.Player();
			if (player == null) return;
			String ActionPanel = arg.Args[0];

			switch(ActionPanel)
            {
								case "page.controller": 
					{
						Int32 Page = Int32.Parse(arg.Args[1]);
						
						Boolean IsModeratorParam;
						if (!Boolean.TryParse(arg.Args[2], out IsModeratorParam)) return;
						
						DrawUI_PlayerPanel(player, Page, IsModeratorParam);
						break;
                    }
                
				case "close.poopup": //report.panel close.poopup
				{
					CuiHelper.DestroyUi(player, "BLURED_POOP_UP");
					CuiHelper.DestroyUi(player, InterfaceBuilder.UI_REPORT_POOPUP_MODERATOR);
					CuiHelper.DestroyUi(player, InterfaceBuilder.UI_REPORT_POOPUP_PLAYER);
					break;
				}
				
                
				case "select.player":
				{
					UInt64 userID;
					if (!UInt64.TryParse(arg.Args[1], out userID)) return;

					Boolean IsModerator;
					if (!Boolean.TryParse(arg.Args[2], out IsModerator)) return;

					BasePlayer Target = IsFake(userID) ? null : BasePlayer.FindByID(userID);
					String displayName = Target == null ? FindFakeName(userID) : Target.displayName;

					if (IsModerator)
					{
						if (Target == null)
						{
							CuiHelper.DestroyUi(player, "BLURED_POOP_UP");
							CuiHelper.DestroyUi(player, InterfaceBuilder.UI_REPORT_POOPUP_MODERATOR);
							DrawUI_PlayerPanel(player, 0, true);
							return;
						}
						DrawUI_PoopUp_Moderator(player, Target);
						return;
					}
					DrawUI_ShowPoopUP(player, displayName, userID.ToString());
					break;
				}

				
				
				case "send.player.report"://report.panel send.player.report userID Reason
				{
					UInt64 TargetID;
					if (!UInt64.TryParse(arg.Args[1], out TargetID))
					{
						CuiHelper.DestroyUi(player, "BLURED_POOP_UP");
						CuiHelper.DestroyUi(player, InterfaceBuilder.UI_REPORT_POOPUP_PLAYER);
						return;
					}

					if (IsFake(TargetID))
					{
						CuiHelper.DestroyUi(player, "BLURED_POOP_UP");
						CuiHelper.DestroyUi(player, InterfaceBuilder.UI_REPORT_POOPUP_PLAYER);
						return;
					}
		   		 		  						  	   		  	   		  	 				   		 		  				
					BasePlayer Target = BasePlayer.FindByID(TargetID);
					if (Target == null)
					{
						CuiHelper.DestroyUi(player, "BLURED_POOP_UP");
						CuiHelper.DestroyUi(player, InterfaceBuilder.UI_REPORT_POOPUP_PLAYER);
						return;
					}

					Int32 ReasonIndex;
					if (!Int32.TryParse(arg.Args[2], out ReasonIndex))
					{
						CuiHelper.DestroyUi(player, "BLURED_POOP_UP");
						CuiHelper.DestroyUi(player, InterfaceBuilder.UI_REPORT_POOPUP_PLAYER);
						return;
					}
					
					SendReportPlayer(player, TargetID, ReasonIndex);
					break;
				}

				
				
				case "select.type.mod": //report.panel select.type.mod boolean
				{
					if (!IsModerator(player)) return;
					
					Boolean IsModeratorParam;
					if (!Boolean.TryParse(arg.Args[1], out IsModeratorParam)) return;

					IsModeratorParam = !IsModeratorParam;
					
					DrawUI_LeftMenu(player, IsModeratorParam);
					DrawUI_PlayerPanel(player, 0, IsModeratorParam);
					DrawUI_HeaderUI_Search(player, IsModeratorParam);
					break;
				}
		   		 		  						  	   		  	   		  	 				   		 		  				
				
				
				case "start.check.player": //report.panel start.check.player ID
				{
					if (!IsModerator(player)) return;

					UInt64 TargetID;
					if (!UInt64.TryParse(arg.Args[1], out TargetID))
					{
						CuiHelper.DestroyUi(player, "BLURED_POOP_UP");
						CuiHelper.DestroyUi(player, InterfaceBuilder.UI_REPORT_POOPUP_MODERATOR);
						return;
					}

					BasePlayer Target = BasePlayer.FindByID(TargetID);
					if (Target == null)
					{
						CuiHelper.DestroyUi(player, "BLURED_POOP_UP");
						CuiHelper.DestroyUi(player, InterfaceBuilder.UI_REPORT_POOPUP_MODERATOR);
						return;
					}

					if (AfkCheckRoutine.ContainsKey(Target))
					{
						if (AfkCheckRoutine[Target] != null)
						{
							ServerMgr.Instance.StopCoroutine(AfkCheckRoutine[Target]);
							AfkCheckRoutine[Target] = null;
					        
							if(PlayerChecks.ContainsKey(Target.userID))
								PlayerChecks.Remove(Target.userID);
						}
					}
			        
					Coroutine routineAFK = ServerMgr.Instance.StartCoroutine(StartAfkCheck(Target, player, false, !config.CheckControllerSettings.UseCheckAFK));

					if (!AfkCheckRoutine.ContainsKey(Target))
						AfkCheckRoutine.Add(Target, routineAFK);
					else AfkCheckRoutine[Target] = routineAFK;
					
					break;
				}

				
				
				case "stop.check.player": //report.panel stop.check.player TargetID
				{
					if (!IsModerator(player)) return;
					UInt64 TargetID;
					if (!UInt64.TryParse(arg.Args[1], out TargetID)) return;

					StopCheckedPlayer(TargetID, player);
					break;
				}

								
				
				case "check.show.verdicts": 
				{
					if (!IsModerator(player)) return;

					UInt64 TargetID;
					if (!UInt64.TryParse(arg.Args[1], out TargetID)) return;
		   		 		  						  	   		  	   		  	 				   		 		  				
					DrawUI_Moderator_Button(player);
					Int32 Y = 0;
					foreach (Configuration.ReasonReport ReasonBan in config.ReasonList)
					{
						DrawUI_Reason_Raiting_Or_Moderator_Menu(player,
							ReasonBan.Title.GetReasonTitle(player.userID),
							InterfaceBuilder.UI_REPORT_MODERATOR_MENU_CHECKED,
							InterfaceBuilder.UI_REPORT_MODERATOR_MENU_CHECKED + $"_REASON_{Y}",
							$"-102.33 {55 + (Y * 30)}", $"102.33 {79 + (Y * 30)}", $"report.panel check.select.verdict {TargetID} {Y}");

						Y++;
					}
					break;
				}

				
				
				case "check.select.verdict":
				{
					if (!IsModerator(player)) return;
		   		 		  						  	   		  	   		  	 				   		 		  				
					UInt64 TargetID;
					if (!UInt64.TryParse(arg.Args[1], out TargetID)) return;

					Int32 VerdictIndex;
					if (!Int32.TryParse(arg.Args[2], out VerdictIndex)) return;
					
					Configuration.ReasonReport Verdict = config.ReasonList[VerdictIndex];
					if (Verdict == null)
					{
						PrintWarning(LanguageEn ? $"We could not find the index of this verdict! Check the configuration or send this message to the developer, the number of verdicts : {config.ReasonList.Count}" : $"Мы не смогли обнаружить индекс данного вердикта! Проверьте конфигурацию или пришлите это сообщение разработчику, количество вердиктов : {config.ReasonList.Count}");
						return;
					}
					
					SendVerdictPlayer(TargetID, player, Verdict);
					break;
				}

				
				
				case "select.raiting.star": 
				{
					Int32 IndexStar;
					if (!Int32.TryParse(arg.Args[1], out IndexStar))
					{
						CuiHelper.DestroyUi(player, InterfaceBuilder.UI_REPORT_RAITING_PLAYER_PANEL);
						return;
					}

					UInt64 ModeratorID;
					if (!UInt64.TryParse(arg.Args[2], out ModeratorID))
					{
						CuiHelper.DestroyUi(player, InterfaceBuilder.UI_REPORT_RAITING_PLAYER_PANEL);
						return;
					}
					
					DrawUI_Raiting_Menu_Stars(player, IndexStar, ModeratorID);

					DrawUI_Reason_Raiting_Or_Moderator_Menu(player,
						GetLang("TITLE_PROFILE_MODERATOR_STATISTICS_TITLE_ONE_ACHIVE", player.UserIDString),
						InterfaceBuilder.UI_REPORT_RAITING_PLAYER_PANEL,
						InterfaceBuilder.UI_REPORT_RAITING_PLAYER_PANEL +
						"_TITLE_PROFILE_MODERATOR_STATISTICS_TITLE_ONE_ACHIVE", $"-102.33 37",
						$"102.33 61.6", $"report.panel select.raiting.reason {IndexStar} {ModeratorID} 1");
					
					DrawUI_Reason_Raiting_Or_Moderator_Menu(player,
						GetLang("TITLE_PROFILE_MODERATOR_STATISTICS_TITLE_TWO_ACHIVE", player.UserIDString),
						InterfaceBuilder.UI_REPORT_RAITING_PLAYER_PANEL,
						InterfaceBuilder.UI_REPORT_RAITING_PLAYER_PANEL +
						"_TITLE_PROFILE_MODERATOR_STATISTICS_TITLE_ONE_ACHIVE", $"-102.33 67", $"102.33 91.6",
						$"report.panel select.raiting.reason {IndexStar} {ModeratorID} 2");
					
					DrawUI_Reason_Raiting_Or_Moderator_Menu(player,
						GetLang("TITLE_PROFILE_MODERATOR_STATISTICS_TITLE_THREE_ACHIVE", player.UserIDString),
						InterfaceBuilder.UI_REPORT_RAITING_PLAYER_PANEL,
						InterfaceBuilder.UI_REPORT_RAITING_PLAYER_PANEL +
						"_TITLE_PROFILE_MODERATOR_STATISTICS_TITLE_ONE_ACHIVE", $"-102.33 97", $"102.33 121.6",
						$"report.panel select.raiting.reason {IndexStar} {ModeratorID} 3");
					break;
				}

				
				
				case "select.raiting.reason": 
				{
					Int32 IndexStar;
					if (!Int32.TryParse(arg.Args[1], out IndexStar))
					{
						CuiHelper.DestroyUi(player, InterfaceBuilder.UI_REPORT_RAITING_PLAYER_PANEL);
						return;
					}
					
					UInt64 ModeratorID;
					if (!UInt64.TryParse(arg.Args[2], out ModeratorID))
					{
						CuiHelper.DestroyUi(player, InterfaceBuilder.UI_REPORT_RAITING_PLAYER_PANEL);
						return;
					}

					Int32 IndexAchive;
					if (!Int32.TryParse(arg.Args[3], out IndexAchive))
					{
						CuiHelper.DestroyUi(player, InterfaceBuilder.UI_REPORT_RAITING_PLAYER_PANEL);
						return;
					}

					SendRaitingModerator(ModeratorID, IndexAchive, IndexStar + 1);	
					CuiHelper.DestroyUi(player, InterfaceBuilder.UI_REPORT_RAITING_PLAYER_PANEL);
					
					SendChat(GetLang("FUNCIONAL_MESSAGE_SEND_RAITING", player.UserIDString), player);

					BasePlayer Moderator = BasePlayer.FindByID(ModeratorID);
					if(Moderator != null)
						SendChat(GetLang("FUNCIONAL_MESSAGE_SEND_RAITING_FOR_MODERATOR", Moderator.UserIDString), Moderator);
					break;
				}
		   		 		  						  	   		  	   		  	 				   		 		  				
								
				
				case "close.select.raiting":  //report.panel close.select.raiting ModeratorID
				{
					UInt64 ModeratorID;
					if (!UInt64.TryParse(arg.Args[1], out ModeratorID))
					{
						CuiHelper.DestroyUi(player, InterfaceBuilder.UI_REPORT_RAITING_PLAYER_PANEL);
						return;
					}
					
					BasePlayer Moderator = BasePlayer.FindByID(ModeratorID);
					if (Moderator != null)
						SendChat(GetLang("FUNCIONAL_MESSAGE_NO_SEND_RAITING_FOR_MODERATOR", Moderator.UserIDString), Moderator);
					else
						Puts(LanguageEn
							? $"Player {player.displayName}({player.userID}) refrained from moderator rating"
							: $"Игрок {player.displayName}({player.userID}) воздержался от оценки модератора");

					
					SendChat(GetLang("FUNCIONAL_MESSAGE_NO_SEND_RAITING", player.UserIDString), player);
					CuiHelper.DestroyUi(player, InterfaceBuilder.UI_REPORT_RAITING_PLAYER_PANEL);
					break;
				}

								
				
				case "search.player": //report.panel search.player SearchName IsModerator
				{
					Boolean IsModerator;
					if (!Boolean.TryParse(arg.Args[1], out IsModerator)) return;
					
					if (!arg.HasArgs(3))
					{
						DrawUI_PlayerPanel(player, IsModerator: IsModerator);
						return;
					}
					
					String SearchName = arg.Args[2];
					if (String.IsNullOrWhiteSpace(SearchName)) return;

					DrawUI_PlayerPanel(player, 0, IsModerator, SearchName);
					break;
				}

				
                default:
                    {
						break;
                    }
            };
        }

        private class ModeratorInformation
        {
	        public Int32 AmountChecked = 0;
	        public Int32 AmountBans = 0;
	        public Int32 AverageRaiting = 0;
	        public List<Int32> OneScore = new List<Int32>();
	        public List<Int32> TwoScore = new List<Int32>();
	        public List<Int32> ThreeScore = new List<Int32>();
	        
	        public Int32 GetAverageRaiting()
	        {
		        Int32 AchiveRaitingOne = GetAverageRaitingAchive(OneScore);
		        Int32 AchiveRaitingTwo = GetAverageRaitingAchive(TwoScore);
		        Int32 AchiveRaitingThree = GetAverageRaitingAchive(ThreeScore);
		        
		        Int32 FullRaiting = AchiveRaitingOne + AchiveRaitingTwo + AchiveRaitingThree;

		        AverageRaiting = FullRaiting / 3;
		        return AverageRaiting;
	        }
	        public Int32 GetAverageRaitingAchive(List<Int32> Score)
	        {
		        Int32 AverageRaitingAchive = 0;
		        Int32 RaitingFull = Score.Sum();
				
		        Int32 FormulDivision = Score.Count == 0 ? 1 : Score.Count;
		        AverageRaitingAchive = RaitingFull / FormulDivision;
		        return AverageRaitingAchive;
	        }
        }
        private Dictionary<UInt64, PlayerRepository> PlayerRepositories = new Dictionary<UInt64, PlayerRepository>();
				
				private class Response
		{
			public List<String> last_ip;
			public String last_nick;
			public List<UInt64> another_accs;
			public List<last_checks> last_check;
			public class last_checks
			{
				public UInt64 moderSteamID;
				public String serverName;
				public Int32 time;
			}
			public List<RustCCBans> bans;
			public class RustCCBans
			{
				public Int32 banID;
				public String reason;
				public String serverName;
				public Int32 OVHserverID;
				public Int32 banDate;
			}
		}
        
        private String VKT_StartCheck(BasePlayer Target, BasePlayer Moderator, Boolean IsConsole = false)
        { 
	        String ModeratorName = !IsConsole && Moderator != null ? Moderator.displayName : "Console";
	        String ModeratorID = !IsConsole && Moderator != null ? Moderator.UserIDString : "Console";
	        String Message = LanguageEn
		        ? $"▣ PLAYER CHECK HAS STARTED ▣\nInformation about the moderator:\n• Nickname: {ModeratorName}\n• Steam64ID: {ModeratorID} (https://steamcommunity.com/profiles/{ModeratorID})\nInformation about the suspect:\n• Nickname: {Target.displayName}\n• Steam64ID: {Target.userID} (https://steamcommunity.com/profiles/{Target.userID})"
		        : $"▣ ЗАПУЩЕНА ПРОВЕРКА ИГРОКА ▣\nИнформация о модераторе :\n• Ник : {ModeratorName}\n• Steam64ID : {ModeratorID} (https://steamcommunity.com/profiles/{ModeratorID})\nИнформация о подозреваемом :\n• Ник : {Target.displayName}\n• Steam64ID : {Target.userID} (https://steamcommunity.com/profiles/{Target.userID})";
		   		 		  						  	   		  	   		  	 				   		 		  				
	        return Message;
        }
		
		private void BanPlayerRCC(UInt64 TargetID, String ReasonBan)
		{
			if (String.IsNullOrWhiteSpace(config.ReferenceSettings.RCCSettings.RCCKey)) return;
			String API = $"https://rustcheatcheck.ru/panel/api?action=addBan&key={config.ReferenceSettings.RCCSettings.RCCKey}&player={TargetID}&reason={ReasonBan}";
			try
			{
				webrequest.Enqueue(API, null, (code, response) => { }, this);
			}
			catch (Exception e)
			{
				PrintError(LanguageEn ? "RCC : We were unable to block the player using RCC, please check if your key is up to date or if RCC is available" : "RCC : Мы не смогли заблокировать игрока с помощью RCC, проверьте актуальность вашего ключа или доступность RCC");
			}
		}

		
		
		
		private Boolean IsRaidBlock(BasePlayer Target)
		{
			if (!config.ReferenceSettings.NoEscapeSetting.NoCheckedRaidBlock) return false;
			if (!NoEscape)
			{
				String ret = Interface.Call("CanTeleport", Target) as String;
				return ret != null;
			}
			Boolean IsRaidBlock = (Boolean)NoEscape.CallHook("IsRaidBlocked", Target);

			return IsRaidBlock;
		}
        private Dictionary<BasePlayer, Coroutine> AfkCheckRoutine = new Dictionary<BasePlayer, Coroutine>();
        private static IQReportSystem _;
		
		private void DrawUI_LeftMenu_Button(BasePlayer player, String TitleButton, String IconButton, String ColorButton, String OffsetMin, String OffsetMax, String Command)
		{
			String Interface = InterfaceBuilder.GetInterface($"{InterfaceBuilder.UI_LAYER}_LEFT_MENU_BUTTON");
			if (Interface == null) return;
			
			Interface = Interface.Replace("%OFFSET_MIN%", OffsetMin);
			Interface = Interface.Replace("%OFFSET_MAX%", OffsetMax);
			Interface = Interface.Replace("%TITLE_BUTTON%", TitleButton);
			Interface = Interface.Replace("%ICON_BUTTON%", IconButton);
			Interface = Interface.Replace("%COLOR_BUTTON%", ColorButton);
			Interface = Interface.Replace("%COMMAND_BUTTON%", Command);

			CuiHelper.AddUi(player, Interface);
		}
        private List<Fields> DT_PlayerMaxReport(String PlayerName, Int32 Reports, UInt64 TargetID)
        {
	        List<Fields> fields = new List<Fields>
	        {
		        new Fields(LanguageEn ? "Information about the suspect :" : "Информация о подозреваемом :", "", false),
		        new Fields("", "", false),
		        new Fields(LanguageEn ? "Nick" : "Ник", $"{PlayerName}", true),
		        new Fields("Steam64ID", $"[{TargetID}](https://steamcommunity.com/profiles/{TargetID})", true),
		        new Fields(LanguageEn ? "Count reports" : "Количество репортов", $"{Reports}", true),
	        };

	        return fields;
        }

        private void RegisteredPlayer(BasePlayer player)
        {
	        if(!PlayerInformations.ContainsKey(player.userID))
		        PlayerInformations.Add(player.userID, new PlayerInformation());

	        if (IsModerator(player))
	        {
		        if (!ModeratorInformations.ContainsKey(player.userID))
			        ModeratorInformations.Add(player.userID, new ModeratorInformation());
	        }
	        else
	        {
		        if(ModeratorInformations.ContainsKey(player.userID))
			        ModeratorInformations.Remove(player.userID);
	        }
        }
		
		
		
		private void DrawUI_TemplatePlayer_Moderator(BasePlayer player, Int32 Y, String UserID, String NickName)
		{
			String Interface = InterfaceBuilder.GetInterface($"{InterfaceBuilder.UI_LAYER}_TEMPLATE_PLAYER_MODERATOR");
			if (Interface == null) return;

			Interface = Interface.Replace("%AVATAR%",GetImage(UserID)); 
			Interface = Interface.Replace("%NAME%", NickName.Length > 7 ? NickName.Substring(0, 7).ToUpper() + ".." : NickName.ToUpper());
			Interface = Interface.Replace("%STEAMID%", UserID);
			Interface = Interface.Replace("%OFFSET_MIN%", $"-219.21 {174.14 - (Y * 62)}");
			Interface = Interface.Replace("%OFFSET_MAX%", $"219.21 {210.14 - (Y * 62)}");
			Interface = Interface.Replace("%REPORT_COUNTS%", $"{PlayerInformations[UInt64.Parse(UserID)].Reports}");
			Interface = Interface.Replace("%COMMAND%", $"report.panel select.player {UserID} true");
			
			Interface = Interface.Replace("%TITLE_PLAYER_NICK_NAME%", GetLang("TITLE_PLAYER_NICK_NAME", player.UserIDString));
			Interface = Interface.Replace("%TITLE_PLAYER_STEAMID%", GetLang("TITLE_PLAYER_STEAMID", player.UserIDString));
			Interface = Interface.Replace("%TITLE_PLAYER_REPORTS%", GetLang("TITLE_PLAYER_REPORTS", player.UserIDString));

			CuiHelper.AddUi(player, Interface);

			if (config.ReferenceSettings.MultiFightingSetting.UseSteamCheck && MultiFighting != null) 
				DrawUI_TemplatePlayer_Moderator_IsSteam(player, UserID);
		}
        
        private void SendSound(UInt64 netId, byte[] data)
        {
	        if (!Net.sv.IsConnected())
		        return;

	        foreach (BasePlayer current in BasePlayer.activePlayerList.Where(current => current.IsConnected))
	        {
		        NetWrite netWrite = Net.sv.StartWrite();
		        netWrite.PacketID(Message.Type.VoiceData);
		        netWrite.UInt64(netId);
		        netWrite.BytesWithSize(data);
		        netWrite.Send(new SendInfo(current.Connection) { priority = Priority.Immediate });
	        }
        }

        public class Fields
        {
            public String name { get; set; }
            public String value { get; set; }
            public bool inline { get; set; }
            public Fields(String name, String value, bool inline)
            {
                this.name = name;
                this.value = value;
                this.inline = inline;
            }
        }
        private Dictionary<UInt64, Timer> TimerWaitChecked = new Dictionary<UInt64, Timer>();
        
        private String VKT_PlayerMaxReport(String PlayerName, Int32 Reports, UInt64 TargetID)
        {
	        String Message = LanguageEn
		        ? $"▣ EXCESS OF COMPLAINTS ▣\nInformation about the suspect:\n• Nickname: {PlayerName}\n• Steam64ID : {TargetID} (https://steamcommunity.com/profiles/{TargetID})\nNumber of reports: {Reports}"
		        : $"▣ ПРЕВЫШЕНИЕ КОЛИЧЕСТВА ЖАЛОБ ▣\nИнформация об подозреваемом :\n• Ник : {PlayerName}\n• Steam64ID : {TargetID} (https://steamcommunity.com/profiles/{TargetID})\nКоличество репортов : {Reports}";

	        return Message;
        }
		   		 		  						  	   		  	   		  	 				   		 		  				
        private List<String> GetTeamsNames(BasePlayer Target)
        {
	        List<String> TeamsName = new List<String>();

	        if (Target.Team == null)
		        return TeamsName;

	        foreach (UInt64 teamMember in Target.Team.members)
	        {
		        IPlayer TeamPlayer = covalence.Players.FindPlayerById(teamMember.ToString());
		        TeamsName.Add(TeamPlayer.Name);
	        }
	        
	        return TeamsName;
        }
		   		 		  						  	   		  	   		  	 				   		 		  				
                
        
        
        private void Init() => ReadData();

		
		private static InterfaceBuilder _interface;
        
        
        public class FancyMessage
        {
            public String content { get; set; }
            public Boolean tts { get; set; }
            public Embeds[] embeds { get; set; }
		   		 		  						  	   		  	   		  	 				   		 		  				
            public class Embeds
            {
                public String title { get; set; }
                public Int32 color { get; set; }
                public List<Fields> fields { get; set; }
                public Authors author { get; set; }

                public Embeds(String title, Int32 color, List<Fields> fields, Authors author)
                {
                    this.title = title;
                    this.color = color;
                    this.fields = fields;
                    this.author = author;

                }
            }

            public FancyMessage(String content, bool tts, Embeds[] embeds)
            {
                this.content = content;
                this.tts = tts;
                this.embeds = embeds;
            }

            public String toJSON() => JsonConvert.SerializeObject(this);
        }
		
		
		
		private void StopDamageAdd(BasePlayer player)
		{
			if (StopDamageMan == null) return;
			if (!config.ReferenceSettings.StopDamageManSetting.UseStopDamage) return;
			
			StopDamageMan.CallHook("AddPlayerSDM", player);
			SendChat(GetLang("FUNCIONAL_PLAYER_STOP_DAMAGE_MAN_ADD", player.UserIDString), player);
		}
		private class Configuration
		{
			[JsonProperty(LanguageEn ? "Setting up compatibility with IQReportSystemr" : "Настройка совместимостей с IQReportSystem")]
			public References ReferenceSettings = new References();
			
			[JsonProperty(LanguageEn ? "Color setting" : "Настройка цветов")]
			public Colors ColorsSettings = new Colors();
			[JsonProperty(LanguageEn ? "Setting images" : "Настройка изображений")]
			public Images ImagesSettings = new Images();
			[JsonProperty(LanguageEn ? "List of reports and reasons for blocking" : "Список жалоб и причин блокировки")]
			public List<ReasonReport> ReasonList = new List<ReasonReport>();
			[JsonProperty(LanguageEn ? "Setting up sending complaints via F7 or the RUST game menu" : "Настройка отправки жалоб через F7 или игровое меню RUST")]
			public ReportF7AndGameMenu ReportF7AndGameMenuSettings = new ReportF7AndGameMenu();
			[JsonProperty(LanguageEn ? "Setting up the Player check process" : "Настройка процесса проверки игрока")]
			public CheckController CheckControllerSettings = new CheckController();
			[JsonProperty(LanguageEn ? "Setting up moderator notifications and the maximum number of reports" : "Настройка уведомления модераторов и максимального количества репортов")]
			public ReportContollerModeration ReportContollerModerationSettings = new ReportContollerModeration();
			[JsonProperty(LanguageEn ? "Setting up sending complaints by players" : "Настройка отправки жалоб игроками")]
			public ReportSendController ReportSendControllerSettings = new ReportSendController();
			[JsonProperty(LanguageEn ? "Additional verdict setting" : "Дополнительная настройка вынесения вердикта")]
			public VerdictController VerdictControllerSettings = new VerdictController();
			[JsonProperty(LanguageEn ? "Configuring notifications for all players about actions in the plugin" : "Настройка уведомлений для всех игроков о действиях в плагине")]
			public NotifyChat NotifyChatSettings = new NotifyChat();
			[JsonProperty(LanguageEn ? "Setting up notifications in Discord" : "Настройка уведомлений в Discord")]
			public NotifyDiscord NotifyDiscordSettings = new NotifyDiscord();
			[JsonProperty(LanguageEn ? "Setting up notifications in VK" : "Настройка уведомлений в VK")]
			public NotifyVK NotifyVKSettings = new NotifyVK();
			[JsonProperty(LanguageEn ? "Command to send data when calling for verification (console and chat)" : "Команда для отправки данных при вызове на проверку (консольная и чатовая)")]
			public String CommandForContact;
			
			internal class VerdictController
			{
				[JsonProperty(LanguageEn ? "Banned all the `Friends` of a player who has been given a verdict by a moderator (true - yes/false - no)" : "Блокировать всех `Друзей` игрока, которому вынес вердикт модератор (true - да/false - нет)")]
				public Boolean UseBanAllTeam;
				[JsonProperty(LanguageEn ? "Index from the list of complaints when blocking a player's `Friends` (From your list - starts from 0)" : "Индекс из списка жалоб при блокировки `Друзей` игрока  (Из вашего списка - начинается от 0)")]
				public Int32 IndexBanReason;
			}
			internal class CheckController
			{
				[JsonProperty(LanguageEn ? "Use sound notification for players when calling for verification (true - yes / false - no) [You must upload sound files to the IQSystem/IQReportSystem/Sounds folder]" : "Использовать звуковое оповещение для игроков при вызове на проверку (true - да/false - нет) [Вы должны загрузить файлы со звуком в папку IQSystem/IQReportSystem/Sounds]")]
				public Boolean UseSoundAlert;
				[JsonProperty(LanguageEn ? "Record a demos of the player during his check" : "Записывать демо игрока во время его проверки")]
				public Boolean UseDemo;
				[JsonProperty(LanguageEn ? "Use AFK validation before calling for check (true - yes / false - no)" : "Использовать проверку на AFK перед вызовом на проверку (true - да/false - нет)")]
				public Boolean UseCheckAFK;
				[JsonProperty(LanguageEn ? "Cancel check for a player automatically with saving reports if he left the server for 15 minutes or more (true - yes/false - no)" : "Отменять проверку игроку автоматически с сохранением репортов если он покинул сервер на 15 минут и более (true - да/false - нет)")]
				public Boolean StopCheckLeavePlayer;

				[JsonProperty(LanguageEn ? "Use tracking of crafting of invaders by the player (will notify the moderator about it) (true - yes / false - no)" : "Использовать отслеживание крафта предметов игроком (будет уведомлять модератора об этом) (true - да/false - нет)")]
				public Boolean TrackCrafting;
				[JsonProperty(LanguageEn ? "Use tracking of messages sent to the chat by the player (will notify the moderator about it) (true - yes / false - no)" : "Использовать отслеживание отправки сообщений в чат игроком (будет уведомлять модератора об этом) (true - да/false - нет)")]
				public Boolean TrackChat;
				[JsonProperty(LanguageEn ? "Use player command usage tracking (will notify the moderator about it) (true - yes / false - no)" : "Использовать отслеживание использования команд игроком (будет уведомлять модератора об этом) (true - да/false - нет)")]
				public Boolean TrackCommand;
			}

			internal class ReportContollerModeration
			{
				[JsonProperty(LanguageEn ? "Maximum number of reports to display the player in the moderator menu and moderator notifications" : "Максимальное количество репортов для отображения игрока в меню модератора и уведомления модератора")]
				public Int32 ReportCountTrigger;
				[JsonProperty(LanguageEn ? "Setting up moderator notifications about the maximum number of player reports" : "Настройка уведомления модератора о максимальном количестве репортов игрока")]
				public AlertModeration AlertModerationSettings = new AlertModeration();
				internal class AlertModeration
				{
					[JsonProperty(LanguageEn ? "Notify the moderator that the player has scored the maximum number of reports" : "Уведомлять модератора о том, что игрок набрал максимальное количество репортов")]
					public Boolean AlertModerator;
					[JsonProperty(LanguageEn ? "Enable an audio notification to the moderator during the notification of the number of reports" : "Включать звуковое уведомление модератору во время уведомления о количестве репортов")]
					public Boolean AlertSound;
					[JsonProperty(LanguageEn ? "The path to the notification sound (this is the path of the prefab of the game - you can see here : https://github.com/OrangeWulf/Rust-Docs/blob/master/Extended/Effects.md)" : "Путь до звука уведомления (это путь префаба игры - посмотреть можно тут : https://github.com/OrangeWulf/Rust-Docs/blob/master/Extended/Effects.md)")]
					public String PathSound;
				}
			}
			internal class ReportSendController
			{
				[JsonProperty(LanguageEn ? "Prohibit a player from sending a complaint against one player several times (true - yes/false - no)" : "Запретить игроку несколько раз отправлять жалобу на одного игрока (true - да/false - нет)")]
				public Boolean NoRepeatReport;
				[JsonProperty(LanguageEn ? "Cooldown before sending a complaint to the players (in seconds) (if you don't need a recharge, leave 0)" : "Перезарядка перед отправкой жалобы на игроков (в секундах) (если вам не нужна перезарядка - оставьте 0)")]
				public Int32 CooldownReport;
				[JsonProperty(LanguageEn ? "Use cooldown before sending a complaint only for a repeated complaint against one player (true) otherwise for all players (false)" : "Использовать перезарядку перед отправкой жалобы только на повторную жалобу на одного игрока (true) иначе на всех игроков (false)")]
				public Boolean CooldownRepeatOrAll;
			}
			internal class ReportF7AndGameMenu
			{
				[JsonProperty(LanguageEn ? "Use sending report via F7 and the RUST game menu (true - yes/false - no)" : "Использовать отправку жалоб через F7 и игровое меню RUST (true - да/false - нет)")]
				public Boolean UseFunction;
				[JsonProperty(LanguageEn ? "Index from the list of complaints when sent via F7 and the RUST game menu (From your list - starts from 0)" : "Индекс из списка жалоб при отправке через F7 и игровое меню RUST (Из вашего списка - начинается от 0)")]
				public Int32 DefaultIndexReason;
			}
			internal class NotifyChat
			{
				[JsonProperty(LanguageEn ? "Notify players when a moderator has started checking a player (configurable in the language file)" : "Уведомлять игроков о том, что модератор начал проверку игрока (настраивается в языковом файле)")]
				public Boolean UseNotifyCheck;
				[JsonProperty(LanguageEn ? "Notify players when a moderator has finished checking a player (configurable in the language file)" : "Уведомлять игроков о том, что модератор завершил проверку игрока (настраивается в языковом файле)")]
				public Boolean UseNotifyStopCheck;
				[JsonProperty(LanguageEn ? "Notify players that the moderator has completed the verification of the player and issued a verdict (banned) (configurable in the language file)" : "Уведомлять игроков о том, что модератор завершил проверку игрока и вынес вердикт (забанил) (настраивается в языковом файле)")]
				public Boolean UseNotifyVerdictCheck;
			}
		   		 		  						  	   		  	   		  	 				   		 		  				
			internal class NotifyVK
			{
				[JsonProperty(LanguageEn ? "Token from the VK group (you can find it in the community settings)" : "Токен от группы ВК (вы можете найти его в настройках сообщества)")]
				public String VKTokenGroup;
				[JsonProperty(LanguageEn ? "ID of the conversation the bot is invited to (countdown starts from 1 - every new conversation +1)" : "ID беседы в которую приглашен бот (отсчет начинается с 1 - каждую новую беседу +1)")]
				public String VKChatID;
			}
			
			internal class NotifyDiscord
			{
				[JsonProperty(LanguageEn ? "Set up WebHooks to send to Discord (if you don't need this feature - leave the field blank)" : "Настройка WebHooks для отправки в Discord (если вам не нужна эта функция - оставьте поле пустым)")]
				public Webhooks WebhooksList = new Webhooks();
				internal class Webhooks
				{
					internal class TemplatesNotify
					{
						[JsonProperty("Webhook")]
						public String WebhookNotify;
						[JsonProperty(LanguageEn ? "Discord message color (Can be found on the website - https://old.message.style/dashboard in the JSON section)" : "Цвет сообщения в Discord (Можно найти на сайте - https://old.message.style/dashboard в разделе JSON)")]
						public Int32 Color; 
						[JsonProperty(LanguageEn ? "Title message" : "Заголовок сообщения")]
						public String AuthorName;
						[JsonProperty(LanguageEn ? "Link to the icon for the avatar of the message" : "Ссылка на иконку для аватарки сообщения")]
						public String IconURL;
		   		 		  						  	   		  	   		  	 				   		 		  				
					}

					[JsonProperty(LanguageEn ? "WebHook : Setting up sending information about the start of a check" : "WebHook : Настройка отправки информации о начале проверки")]
					public TemplatesNotify NotifyStartCheck = new TemplatesNotify(); 
					
					[JsonProperty(LanguageEn ? "WebHook : Setting up sending information about the stop of a check" : "WebHook : Настройка отправки информации о завершении проверки")]
					public TemplatesNotify NotifyStopCheck = new TemplatesNotify(); 

					[JsonProperty(LanguageEn ? "WebHook : Settings for sending player contact information (when a player sends their Discord)" : "WebHook : Настройка отправки информации о контактах игрока (когда игрок отправляет свой Discord)")]
					public TemplatesNotify NotifyContacts = new TemplatesNotify(); 
					
					[JsonProperty(LanguageEn ? "WebHook : Setting up sending information about player complaints" : "WebHook : Настройка отправки информации о жалобах игроков")]
					public TemplatesNotify NotifySendReport = new TemplatesNotify();
					
					[JsonProperty(LanguageEn ? "WebHook : Setting up sending information when a player has exceeded the maximum number of complaints" : "WebHook : Настройка отправки информации когда игрок превысил максимальное количество жалоб")]
					public TemplatesNotify NotifyMaxReport = new TemplatesNotify();
					
					[JsonProperty(LanguageEn ? "WebHook : Setting up sending information about changing the status of the player and moderator" : "WebHook : Настройка отправки информации о изменении статуса игрока и модератора")]
					public TemplatesNotify NotifyStatusPlayerOrModerator = new TemplatesNotify();
				}
			}
			internal class ReasonReport
			{
				[JsonProperty(LanguageEn ? "Reason" : "Причина")]
				public LangText Title;
				[JsonProperty(LanguageEn ? "The command from your ban system to block the user ({0} - will be replaced by the player's ID)" : "Команда вашей бан-системы на блокировку пользователя ({0} - заменится на ID игрока)")]
				public String BanCommand;
				[JsonProperty(LanguageEn ? "Hide this reason from the player (true) (will only be seen by the moderator when passing a verdict)" : "Скрыть данную причину от игрока (true) (будет видеть только модератор при вынесении вердикта)")]
				public Boolean HideUser;
			}
			internal class Images
			{
				[JsonProperty(LanguageEn ? "Reports section images" : "Изображения раздела для жалоб")]
				public PlayerListBlock PlayerListBlockSettings = new PlayerListBlock();
				
				[JsonProperty(LanguageEn ? "Images of the statistics section" : "Изображения раздела статистики")]
				public StatisticsBlock StatisticsBlockSettings = new StatisticsBlock();

				[JsonProperty(LanguageEn ? "Left menu images" : "Изображения левого меню")]
				public LeftBlock LeftBlockSettings = new LeftBlock();
				
				[JsonProperty(LanguageEn ? "Moderation section images" : "Изображения раздела модерации")]
				public ModerationBlock ModerationBlockSettings = new ModerationBlock();
				[JsonProperty(LanguageEn ? "Moderator menu images when checking" : "Изображения меню модератора при проверки")]
				public ModeratorMenuChecked ModeratorMenuCheckedSettings = new ModeratorMenuChecked();
				
				[JsonProperty(LanguageEn ? "Images of the player rating menu quality of the moderator's work" : "Изображения меню оценки игроком качество работы модератора")]
				public PlayerMenuRaiting PlayerMenuRaitingSettings = new PlayerMenuRaiting();

				[JsonProperty(LanguageEn ? "PNG of the menu background (1382x950)" : "PNG заднего фона меню (1382x950)")]
				public String Background;
				[JsonProperty(LanguageEn ? "PNG down arrows (page flipping) (10x5)" : "PNG стрелки вниз(перелистывание страниц) (10x5)")]
				public String PageDown;
				[JsonProperty(LanguageEn ? "PNG up arrows (page flipping) (10x5)" : "PNG стрелки вверх(перелистывание страниц) (10x5)")]
				public String PageUp;	
				[JsonProperty(LanguageEn ? "PNG icon search (16x16)" : "PNG иконки поиска (16x16)")]
				public String Search;
				[JsonProperty(LanguageEn ? "PNG : Icon for adjusting avatar (64x64)" : "PNG : Иконка для корректировки автарки (64x64)")]
				public String AvatarBlur;
				[JsonProperty(LanguageEn ? "PNG : Icon for moderation verdict or rating (307x36)" : "PNG : Иконка для вердикта модерации или рейтинга (307x36)")]
				public String ReasonModeratorAndRaiting; 
				[JsonProperty(LanguageEn ? "PNG : Image on the player's screen with text about the start of the checks (1450x559)" : "PNG : Изображение на экране игрока с текстом о начале проверки (1450x559)")]
				public String PlayerAlerts;
				
				internal class PlayerMenuRaiting
				{
					[JsonProperty(LanguageEn ? "PNG : Menu background when evaluating a reviewer (307x98)" : "PNG : Задний фон меню при оценке проверяющего (307x98)")]
					public String PlayerMenuRaitingBackground; 
				}
				internal class ModeratorMenuChecked
				{
					[JsonProperty(LanguageEn ? "PNG : Menu background when checking player (307x148)" : "PNG : Задний фон меню при проверки игрока (307x148)")]
					public String ModeratorCheckedBackground;
					[JsonProperty(LanguageEn ? "PNG : End test button (128x40)" : "PNG : Кнопка завершения проверки (128x40)")]
					public String ModeratorCheckedStopButton;	
					[JsonProperty(LanguageEn ? "PNG : Verdict button (128x40)" : "PNG : Кнопка вердикта (128x40)")]
					public String ModeratorVerdictButton;
					[JsonProperty(LanguageEn ? "PNG : `Licenses` icon (if Lumia support is enabled) (16x16)" : "PNG : Иконка `Лицензии` (если включена поддержка Luma) (16x16)")]
					public String SteamIcoPlayer;
					[JsonProperty(LanguageEn ? "PNG : `Pirate` icon (if Lumia support is enabled) (16x16)" : "PNG : Иконка `Пират` (если включена поддержка Luma) (16x16)")]
					public String PirateIcoPlayer;
				}
				internal class ModerationBlock
				{
					[JsonProperty(LanguageEn ? "PNG : Player information popup background (831x599)" : "PNG : Задний фон всплывающего окна с информацией о игроке (831x599)")]
					public String ModeratorPoopUPBackgorund;
					[JsonProperty(LanguageEn ? "PNG : Element background for text (155x22)" : "PNG : Задний фон элемента для текста (155x22)")]
					public String ModeratorPoopUPTextBackgorund;
					[JsonProperty(LanguageEn ? "PNG : Information panel background (196x240)" : "PNG : Задний фон панели с информацией (196x240)")]
					public String ModeratorPoopUPPanelBackgorund;
				}

				internal class LeftBlock
				{
					[JsonProperty(LanguageEn ? "PNG : Icon for sidebar button (192x55)" : "PNG : Иконка для кнопки в боковом меню (192x55)")]
					public String ButtonBackgorund;
					[JsonProperty(LanguageEn ? "PNG : Icon for the button in `reports` (32x32)" : "PNG : Иконка для кнопки в `жалобы`(32x32)")]
					public String ReportIcon;
					[JsonProperty(LanguageEn ? "PNG : Icon for the button in `moderation` (32x32)" : "PNG : Иконка для кнопки в `модерация`(32x32)")]
					public String ModerationIcon;
				}
				
				internal class PlayerListBlock
				{
					[JsonProperty(LanguageEn ? "PNG : Cause selection popup background (567x599)" : "PNG : Задний фон всплывающего сообщения с выбором причины (567x599)")]
					public String PoopUpBackgorund;
					[JsonProperty(LanguageEn ? "PNG : Reason background in popup (463x87)" : "PNG : Задний фон причины в всплывающем окне (463x87)")]
					public String PoopUpReasonBackgorund;
				}

				internal class StatisticsBlock
                {
					[JsonProperty(LanguageEn ? "PNG Background of the statistics block (283x81)" : "PNG Задний фон блока статистики (283x81)")]
					public String BlockStatsModeration;		
					[JsonProperty(LanguageEn ? "PNG Background of the rating block in statistics (65x28)" : "PNG Задний фон блока рейтинга в статистике (65x28)")]
					public String BlockStatsRaitingModeration;
					[JsonProperty(LanguageEn ? "PNG : Rating icon in statistics (16x15)" : "PNG : Иконка рейтинга в статистике (16x15)")]
					public String RaitingImage;
				}
			}
		   		 		  						  	   		  	   		  	 				   		 		  				
			internal class Colors
			{
				[JsonProperty(LanguageEn ? "RGBA of the main text color" : "RGBA основного цвета текста")]
				public String MainColorText;
				[JsonProperty(LanguageEn ? "RGBA of additional text color" : "RGBA дополнительного цвета текста")]
				public String AdditionalColorText;	
				[JsonProperty(LanguageEn ? "RGBA additional color of elements (Buttons, dies)" : "RGBA дополнительный цвет элементов (Кнопки, плашки)")]
				public String AdditionalColorElements;
				[JsonProperty(LanguageEn ? "RGBA additional color of elements (Buttons, dies) #2" : "RGBA дополнительный цвет элементов (Кнопки, плашки) #2")]
				public String AdditionalColorElementsTwo;	
				[JsonProperty(LanguageEn ? "RGBA additional color of elements (Buttons, dies) #3" : "RGBA дополнительный цвет элементов (Кнопки, плашки) #3")]
				public String AdditionalColorElementsThree;	
				[JsonProperty(LanguageEn ? "RGBA the main color" : "RGBA основной цвет")]
				public String MainColor;
			}
			internal class References
            {
				[JsonProperty(LanguageEn ? "IQFakeActive : Use collaboration (true - yes/false - no)" : "IQFakeActive : Использовать совместную работу (true - да/false - нет)")]
				public Boolean IQFakeActiveUse;
				[JsonProperty(LanguageEn ? "Setting up IQChat" : "Настройка IQChat")]
				public IQChatReference IQChatSetting = new IQChatReference();
				[JsonProperty(LanguageEn ? "Setting up NoEscape" : "Настройка NoEscape")]
				public NoEscapeReference NoEscapeSetting = new NoEscapeReference();
				[JsonProperty(LanguageEn ? "Duels : Reschedule the player's check if he is in a duel (true - yes/false - no)" : "Duels : Перенести проверку игрока если он на дуэли (true - да/false - нет)")]
				public Boolean NoCheckedDuel;
				[JsonProperty(LanguageEn ? "Setting up Friends" : "Настройка Friends")]
				public FriendsReference FriendsSetting = new FriendsReference();
				[JsonProperty(LanguageEn ? "Setting up Clans" : "Настройка Clans")]
				public ClansReference ClansSetting = new ClansReference();
				[JsonProperty(LanguageEn ? "Setting up MultiFighting (Luma)" : "Настройка MultiFighting (Luma)")]
				public MultiFighting MultiFightingSetting = new MultiFighting();
				[JsonProperty(LanguageEn ? "Setting up StopDamageMan" : "Настройка StopDamageMan")]
				public StopDamageMan StopDamageManSetting = new StopDamageMan();
				
				[JsonProperty(LanguageEn ? "Setting up RCC support" : "Настройка поддержки RCC")]
				public RustCheatCheck RCCSettings = new RustCheatCheck();
				[JsonProperty(LanguageEn ? "Setting up OzProtect support" : "Настройка поддержки OzProtect")]
				public OzProtectCheck OzProtectSettings = new OzProtectCheck();

				internal class MultiFighting
				{
					[JsonProperty(LanguageEn ? "MultiFighting (Luma) : Display an icon with the player status - `Steam` / `Pirate`" : "MultiFighting (Luma) : Отображать иконку со статусом игрока - `Steam` / `Пират`")]
					public Boolean UseSteamCheck;
				}

				internal class StopDamageMan
				{
					[JsonProperty(LanguageEn ? "StopDamageMan : Disable player damage during check" : "StopDamageMan : Отключать игроку урон во время проверки")]
					public Boolean UseStopDamage;
				}
				
				internal class RustCheatCheck
				{
					[JsonProperty(LanguageEn ? "RCC key (if you don't need RCC support, leave the key blank)" : "Ключ RCC (если вам не нужна поддержка RCC - оставьте ключ пустым)")]
					public String RCCKey;
				}
				
				internal class OzProtectCheck
				{
					[JsonProperty(LanguageEn ? "OzProtect key (if you don't need OzProtect support, leave the key blank)" : "Ключ OzProtect (если вам не нужна поддержка OzProtect - оставьте ключ пустым)")]
					public String OzProtectKey;
				}
				
				internal class IQChatReference
				{
					[JsonProperty(LanguageEn ? "IQChat : Custom prefix in chat" : "IQChat : Кастомный префикс в чате")]
					public String CustomPrefix;
					[JsonProperty(LanguageEn ? "IQChat : Custom chat avatar (If required)" : "IQChat : Кастомный аватар в чате(Если требуется)")]
					public String CustomAvatar;
					[JsonProperty(LanguageEn ? "IQChat : Use UI notification (true - yes/false - no)" : "IQChat : Использовать UI уведомление (true - да/false - нет)")]
					public Boolean UIAlertUse;
				}

				internal class NoEscapeReference
				{
					[JsonProperty(LanguageEn ? "NoEscape : Reschedule a player's check if he has a `Raid-Block` (true - yes/false - no)" : "NoEscape : Перенести проверку игрока если у него есть `Raid-Блок` (true - да/false - нет)")]
					public Boolean NoCheckedRaidBlock;
					[JsonProperty(LanguageEn ? "NoEscape : Reschedule a player's check if he has a `Combat-Block` (true - yes/false - no)" : "NoEscape : Перенести проверку игрока если у него есть `Комбат-Блок` (true - да/false - нет)")]
					public Boolean NoCheckedCombatBlock;
				}

				internal class FriendsReference
				{
					[JsonProperty(LanguageEn ? "Friends : Prohibit players in the team from sending reports to each other (true - yes/false - no)" : "Friends : Запретить игрокам в команде отправлять репорты друг на друга (true - да/false - нет)")]
					public Boolean SendReportFriend;
					[JsonProperty(LanguageEn ? "Friends : Prohibit the moderator from checking his teammate (true - yes/false - no)" : "Friends : Запретить модератору проверять своего тиммейта (true - да/false - нет)")]
					public Boolean StartCheckedFriend;
				}
				
				internal class ClansReference
				{
					[JsonProperty(LanguageEn ? "Clans : Prohibit players in the same clan from sending reports to each other (true - yes/false - no)" : "Clans : Запретить игрокам в одном клане отправлять репорты друг на друга (true - да/false - нет)")]
					public Boolean SendReportClan;
					[JsonProperty(LanguageEn ? "Clans : Prohibit the moderator from checking the members of his clan (true - yes/false - no)" : "Clans : Запретить модератору проверять участников своего клана (true - да/false - нет)")]
					public Boolean StartCheckedClan;
				}
			}
			
			internal class LangText
			{
				[JsonProperty(LanguageEn ? "Reason title russian" : "Причина на русском")]
				public String LanguageRU;
				[JsonProperty(LanguageEn ? "Reason title english" : "Причина на английском")]
				public String LanguageEN;

				public String GetReasonTitle(UInt64 TargetID) =>
					_.lang.GetLanguage(TargetID.ToString()).Equals("ru") ? LanguageRU : LanguageEN;
			}

			public static Configuration GetNewConfiguration()
			{
				return new Configuration
				{
					CommandForContact = "discord",
					VerdictControllerSettings = new VerdictController()
					{
						UseBanAllTeam = false,
						IndexBanReason = 5,
					},
					CheckControllerSettings = new CheckController
					{
						StopCheckLeavePlayer = false,
						UseDemo = true,
						UseSoundAlert = false,
						UseCheckAFK = true,
						TrackCrafting = false,
						TrackChat = false,
						TrackCommand = false,
					},
					NotifyChatSettings = new NotifyChat()
					{
						UseNotifyCheck = true,
						UseNotifyStopCheck = true,
						UseNotifyVerdictCheck = true,
					},
					NotifyVKSettings = new NotifyVK()
					{
						VKTokenGroup = "",
						VKChatID = ""
					},
					NotifyDiscordSettings = new NotifyDiscord()
					{
						WebhooksList = new NotifyDiscord.Webhooks()
						{
							NotifySendReport = new NotifyDiscord.Webhooks.TemplatesNotify()
							{
								WebhookNotify = "",
								Color = 16728083,
								AuthorName = LanguageEn ? "NEW REPORT" : "НОВАЯ ЖАЛОБА",
								IconURL = "https://cdn.discordapp.com/attachments/1139598345682305164/1141662192735883304/2N5je4x.jpg",
							},
							NotifyContacts = new NotifyDiscord.Webhooks.TemplatesNotify()
							{
								WebhookNotify = "",
								Color = 13850622,
								AuthorName = LanguageEn ? "PROVIDED CONTACTS" : "ПРЕДОСТАВЛЕННЫЕ КОНТАКТЫ",
								IconURL = "https://cdn.discordapp.com/attachments/1139598345682305164/1141662339565883464/bGJtYB5.jpg",
							},
							NotifyStartCheck = new NotifyDiscord.Webhooks.TemplatesNotify
							{
								WebhookNotify = "",
								Color = 16755200,
								AuthorName = LanguageEn ? "PLAYER CHECK" : "ПРОВЕРКА ИГРОКА",
								IconURL = "https://cdn.discordapp.com/attachments/1139598345682305164/1141662192735883304/2N5je4x.jpg",
							},
							NotifyStopCheck = new NotifyDiscord.Webhooks.TemplatesNotify()
							{
								WebhookNotify = "",
								Color = 7846721,
								AuthorName = LanguageEn ? "COMPLETE CHECK" : "ЗАВЕРШЕННАЯ ПРОВЕРКА",
								IconURL = "https://cdn.discordapp.com/attachments/1139598345682305164/1141662457719423076/VKwsjXO.jpg",
							},
							NotifyMaxReport = new NotifyDiscord.Webhooks.TemplatesNotify()
							{
								WebhookNotify = "",
								Color = 16728083,
								AuthorName = LanguageEn ? "MAXIMUM NUMBER OF COMPLAINTS" : "МАКСИМАЛЬНОЕ КОЛИЧЕСТВО ЖАЛОБ",
								IconURL = "https://cdn.discordapp.com/attachments/1139598345682305164/1141662192735883304/2N5je4x.jpg",
							},
							NotifyStatusPlayerOrModerator = new NotifyDiscord.Webhooks.TemplatesNotify()
							{
								WebhookNotify = "",
								Color = 16752000,
								AuthorName = LanguageEn ? "CONNECTION STATUS CHANGES" : "ИЗМЕНЕНИЯ СТАТУСА ПОДКЛЮЧЕНИЯ",
								IconURL = "https://cdn.discordapp.com/attachments/1139598345682305164/1141662192735883304/2N5je4x.jpg",
							}
						}
					},
					ReasonList = new List<ReasonReport>()
					{
						new ReasonReport
						{
							Title = new LangText()
							{
								LanguageRU = "Подозрительный",
								LanguageEN = "Suspicious"
							},
							BanCommand = "ban {0} 1d {1}",
							HideUser = false,
						},
						new ReasonReport
						{
							Title = new LangText()
							{
								LanguageRU = "Макросы",
								LanguageEN = "Macros"
							},
							BanCommand = "ban {0} 14d {1}",
							HideUser = false,
						},
						new ReasonReport
						{
							Title = new LangText()
							{
								LanguageRU = "Читер",
								LanguageEN = "Cheater"
							},
							BanCommand = "ban {0} {1}",
							HideUser = false,
						},
						new ReasonReport
						{
							Title = new LangText()
							{
								LanguageRU = "3+",
								LanguageEN = "3+"
							},
							BanCommand = "ban {0} 7d {1}",
							HideUser = false,
						},
						new ReasonReport
						{
							Title = new LangText()
							{
								LanguageRU = "Отказ от проверки",
								LanguageEN = "Refusal to check"
							},
							BanCommand = "ban {0} 7d {1}",
							HideUser = true,
						},
						new ReasonReport
						{
							Title = new LangText()
							{
								LanguageRU = "Игра с нарушителем",
								LanguageEN = "Playing with the intruder"
							},
							BanCommand = "ban {0} 3d {1}",
							HideUser = false,
						},
					},
					ReportF7AndGameMenuSettings = new ReportF7AndGameMenu()
					{
						UseFunction	= true,
						DefaultIndexReason = 2,
					},
					ReportSendControllerSettings = new ReportSendController()
					{
						CooldownRepeatOrAll	= false,
						CooldownReport = 300,
						NoRepeatReport = true,
					},
					ReportContollerModerationSettings = new ReportContollerModeration()
					{
						ReportCountTrigger = 1,
						AlertModerationSettings = new ReportContollerModeration.AlertModeration
						{
							AlertModerator = true,
							AlertSound = true,
							PathSound = "assets/prefabs/npc/autoturret/effects/targetacquired.prefab"
						}
					},
					ColorsSettings = new Colors()
					{
						MainColorText = "1 1 1 1", 
						AdditionalColorText = "1 1 1 0.7", 
						AdditionalColorElements = "0.20 0.22 0.25 1",  
						MainColor = "0.12 0.14 0.16 1",
						AdditionalColorElementsTwo = "0.28 0.27 0.45 1",
						AdditionalColorElementsThree = "0.30 0.77 0.99 1",
					},
					ImagesSettings = new Images
                    {
	                    Background = "https://cdn.discordapp.com/attachments/1139598345682305164/1141662652804907109/0N6OgXu.png",
						PageDown = "https://cdn.discordapp.com/attachments/1139598345682305164/1141662783973380307/fYZKtuF.png",
						PageUp = "https://cdn.discordapp.com/attachments/1139598345682305164/1141663014928535572/P17Rrjp.png",
						Search = "https://cdn.discordapp.com/attachments/1139598345682305164/1141663109560414290/yVBF1X7.png",
						AvatarBlur = "https://cdn.discordapp.com/attachments/1139598345682305164/1141663233644699698/111.png",
						ReasonModeratorAndRaiting = "https://cdn.discordapp.com/attachments/1139598345682305164/1141663550239162419/Reason-Moderator.png",
						PlayerAlerts = "https://cdn.discordapp.com/attachments/1139598345682305164/1141663660121538580/XMeNBAj.png",
						
						PlayerMenuRaitingSettings = new Images.PlayerMenuRaiting()
						{
							PlayerMenuRaitingBackground = "https://cdn.discordapp.com/attachments/1139598345682305164/1141663937268559962/lLOWzFo.png",
						},
						
						ModeratorMenuCheckedSettings = new Images.ModeratorMenuChecked()
						{
							ModeratorCheckedBackground	= "https://cdn.discordapp.com/attachments/1139598345682305164/1141664408507011083/nBXA5xm_1.png",
							ModeratorCheckedStopButton = "https://cdn.discordapp.com/attachments/1139598345682305164/1141664510961270826/osbdHca.png",
							ModeratorVerdictButton = "https://cdn.discordapp.com/attachments/1139598345682305164/1141664672618139718/1vthtRD.png",
							SteamIcoPlayer = "https://cdn.discordapp.com/attachments/1139598345682305164/1141664792822681620/htRHWNV.png",
							PirateIcoPlayer = "https://cdn.discordapp.com/attachments/1139598345682305164/1141664930190331944/GLZQK5a.png",
						},
						
						ModerationBlockSettings = new Images.ModerationBlock()
						{
							ModeratorPoopUPBackgorund = "https://cdn.discordapp.com/attachments/1139598345682305164/1141678266688733231/aqjRQqw.png",
							ModeratorPoopUPTextBackgorund = "https://cdn.discordapp.com/attachments/1139598345682305164/1141678366622236772/ZYEcVG2.png",
							ModeratorPoopUPPanelBackgorund = "https://cdn.discordapp.com/attachments/1139598345682305164/1141678463317717094/1eSlL3s.png",
						},
						
						LeftBlockSettings = new Images.LeftBlock
						{
							ButtonBackgorund = "https://cdn.discordapp.com/attachments/1139598345682305164/1141678555877605406/wgYl90C.png",
							ReportIcon = "https://cdn.discordapp.com/attachments/1139598345682305164/1141678646831095860/mayvxja.png",
							ModerationIcon = "https://cdn.discordapp.com/attachments/1139598345682305164/1141678744944246864/UXhlKHq.png",
						},
						
						PlayerListBlockSettings = new Images.PlayerListBlock()
						{
							PoopUpBackgorund = "https://cdn.discordapp.com/attachments/1139598345682305164/1141679152160854026/6TGBXIv.png",
							PoopUpReasonBackgorund = "https://cdn.discordapp.com/attachments/1139598345682305164/1141679349788065853/eq76Zc0.png",
						},
						StatisticsBlockSettings = new Images.StatisticsBlock
                        {
							BlockStatsModeration = "https://cdn.discordapp.com/attachments/1139598345682305164/1141679519275696188/iPobuoo.png",
							BlockStatsRaitingModeration = "https://cdn.discordapp.com/attachments/1139598345682305164/1141679717913743391/B17soOi.png",
							RaitingImage = "https://cdn.discordapp.com/attachments/1139598345682305164/1141679787929243768/2Vo6MQ8.png",
						}
					},
					ReferenceSettings = new References
					{
						IQFakeActiveUse = false,
						StopDamageManSetting = new References.StopDamageMan()
						{
							UseStopDamage = false,
						},
						MultiFightingSetting = new References.MultiFighting()
						{
							UseSteamCheck = false,
						},
						IQChatSetting = new References.IQChatReference
						{
							CustomPrefix = "[<color=#FF4B42>IQReportSystem</color>]\n",
							CustomAvatar = "0",
							UIAlertUse = false,
						},
						NoEscapeSetting = new References.NoEscapeReference()
						{
							NoCheckedRaidBlock	= true,
							NoCheckedCombatBlock = false,
						},
						NoCheckedDuel = true,
						FriendsSetting = new References.FriendsReference()
						{
							SendReportFriend = true,
							StartCheckedFriend = true,
						},
						ClansSetting = new References.ClansReference()
						{
							SendReportClan = true,
							StartCheckedClan = true,
						},
						RCCSettings = new References.RustCheatCheck()
						{
							RCCKey = "",
						},
						OzProtectSettings = new References.OzProtectCheck()
						{
							OzProtectKey = "",
						}
					}
				};
			}
		}
		
		private void DrawUI_Moderator_Checked_Menu_Status(BasePlayer moderator, String Status)
		{
			String Interface = InterfaceBuilder.GetInterface($"{InterfaceBuilder.UI_LAYER}_ONLINE_STATUS_CHECKED_MODERATOR");
			if (Interface == null) return;

			Interface = Interface.Replace("%TITLE_PROFILE_MODERATOR_CHECKED_MENU_TITLE_STATUS%", GetLang("TITLE_PROFILE_MODERATOR_CHECKED_MENU_TITLE_STATUS", moderator.UserIDString, Status));
		   		 		  						  	   		  	   		  	 				   		 		  				
			CuiHelper.DestroyUi(moderator, "InfoStatus");
			CuiHelper.AddUi(moderator, Interface);
		}
        private void SteamAvatarAdd(String userid)
        {
	        if (ImageLibrary == null)
		        return;
	        if (HasImage(userid))
		        return;
	        webrequest.Enqueue($"https://steamcommunity.com/profiles/{userid}?xml=1", null, 
		        (code, response) =>
		        {
			        if (response == null || code != 200) 
				        return;
                    
			        String avatarUrl = _avatarRegex.Match(response).Groups[1].ToString();
			        if (!String.IsNullOrEmpty(avatarUrl))
			        {
				        ImageLibrary?.Call("AddImage", avatarUrl, userid);
			        }
		        }, this);
        }

                
        
        private void PreLoadedPlugin()
        {
			if(!ImageLibrary)
			{
				NextTick(() => {
					PrintError($"ImageLibrary not found! Please, check your plugins list.");
					Interface.Oxide.UnloadPlugin(Name);
				});
				return;	
			}
        }
		public string GetLang(string LangKey, string userID = null, params object[] args)
		{
			sb.Clear();
			if (args != null)
			{
				sb.AppendFormat(lang.GetMessage(LangKey, this, userID), args);
				return sb.ToString();
			}
			return lang.GetMessage(LangKey, this, userID);
		}
		
		void OnItemCraftCancelled(ItemCraftTask task, ItemCrafter crafter)
		{
			BasePlayer player = crafter.owner;
			if (player == null) return;
			
			if (!PlayerChecks.ContainsKey(player.userID)) return;

			if (config.CheckControllerSettings.TrackCrafting)
			{
				BasePlayer Moderator = BasePlayer.FindByID(PlayerChecks[player.userID].ModeratorID);
				if (Moderator != null)
					SendChat(GetLang("FUNCIONAL_MESSAGE_CHECK_AFK_PLAYER_CANCELLED_CRAFTING", Moderator.UserIDString), Moderator);
			}

			player.ResetInputIdleTime();;
		}
		
        private Boolean IsModerator(BasePlayer moderator) =>
	        permission.UserHasPermission(moderator.UserIDString, ModeratorPermissions);

        
        
        [ChatCommand("report")]
        private void ChatCommandReport(BasePlayer player, String cmd, String[] args)
        {
			if (player == null) return;
			if (_interface == null) return;
            DrawUI_Report_Panel(player);
        }

		void OnPlayerCommand(BasePlayer player, string command, string[] args)
		{
			if (!PlayerChecks.ContainsKey(player.userID)) return;
			
			if (config.CheckControllerSettings.TrackCommand)
			{
				BasePlayer Moderator = BasePlayer.FindByID(PlayerChecks[player.userID].ModeratorID);
				if (Moderator != null)
					SendChat(GetLang("FUNCIONAL_MESSAGE_CHECK_AFK_PLAYER_SEND_COMMAND", Moderator.UserIDString, command), Moderator);
			}

			player.ResetInputIdleTime();
		}
        private List<Fields> DT_PlayerSendReport(BasePlayer Sender, UInt64 TargetID, String Reason)
        {
	        List<Fields> fields = new List<Fields>
	        {
		        new Fields(LanguageEn ? "New complaint received :" : "Получена новая жалоба :", "", false),
		        new Fields("", "", false),
		        new Fields(LanguageEn ? "Information about the sender :" : "Информация об отправителе :", "", false),
		        new Fields("", "", false),
		        new Fields(LanguageEn ? "Nick" : "Ник", $"{Sender.displayName}", true),
		        new Fields("Steam64ID", $"[{Sender.userID}](https://steamcommunity.com/profiles/{Sender.userID})", true),
		        new Fields("", "", false),
		        new Fields(LanguageEn ? "Information about the suspect :" : "Информация о подозреваемом :", "", false),
		        new Fields("", "", false),
		        new Fields(LanguageEn ? "Nick" : "Ник", $"{covalence.Players.FindPlayerById(TargetID.ToString()).Name ?? "EMPTY"}", true),
		        new Fields("Steam64ID", $"[{TargetID}](https://steamcommunity.com/profiles/{TargetID})", true),
		        new Fields(LanguageEn ? "Reason for complaint :" : "Причина жалобы :", Reason, false),
	        };
	        
	        return fields;
        }

        
                
        private Dictionary<UInt64, PlayerInformation> PlayerInformations = new Dictionary<UInt64, PlayerInformation>();
        
        private void RunEffect(BasePlayer Moderator)
        {
	        if (!config.ReportContollerModerationSettings.AlertModerationSettings.AlertSound || String.IsNullOrWhiteSpace(config.ReportContollerModerationSettings.AlertModerationSettings.PathSound)) return;
	        Effect effect = new Effect(config.ReportContollerModerationSettings.AlertModerationSettings.PathSound, Moderator, 0, new Vector3(), new Vector3());
	        EffectNetwork.Send(effect, Moderator.Connection);
        }

		
		
		private void DrawUI_Raiting_Menu_Player(BasePlayer player, UInt64 ModeratorID)
		{
			String Interface = InterfaceBuilder.GetInterface($"{InterfaceBuilder.UI_LAYER}_RAITING_MENU_PLAYER");
			if (Interface == null) return;
			
			IPlayer iModerator = covalence.Players.FindPlayerById(ModeratorID.ToString());
			String ModeratorName = iModerator == null ? GetLang("TITLE_RAITING_WORK_MODERATOR_WHO_MODERATOR_NOT_NAME", player.UserIDString) : iModerator.Name;

			Interface = Interface.Replace("%TITLE_CLOSE_BUTTON_REPORT%", GetLang("TITLE_CLOSE_BUTTON_REPORT", player.UserIDString));
			Interface = Interface.Replace("%TITLE_RAITING_WORK_MODERATOR_TITLE%", GetLang("TITLE_RAITING_WORK_MODERATOR_TITLE", player.UserIDString));
			Interface = Interface.Replace("%TITLE_RAITING_WORK_MODERATOR_WHO_MODERATOR%", GetLang("TITLE_RAITING_WORK_MODERATOR_WHO_MODERATOR", player.UserIDString, ModeratorName.ToUpper()));
			Interface = Interface.Replace("%COMMAND_CLOSE%", $"report.panel close.select.raiting {ModeratorID}");

			CuiHelper.AddUi(player, Interface);
			
			DrawUI_Raiting_Menu_Stars(player, -1, ModeratorID);
		}
        
        private const String ModeratorPermissions = "iqreportsystem.moderation";
		
				void SyncReservedFinish(String JSON)
		{
			if (!config.ReferenceSettings.IQFakeActiveUse) return;
			List<FakePlayer> ContentDeserialize = JsonConvert.DeserializeObject<List<FakePlayer>>(JSON);
			PlayerBases = ContentDeserialize;
			
			foreach (FakePlayer fakePlayer in PlayerBases)
				SteamAvatarAdd(fakePlayer.UserID.ToString());

			PrintWarning("IQReportSystem - успешно синхронизирована с IQFakeActive");
			PrintWarning("=============SYNC==================");
		}
        
        private NpcSound LoadDataSound(String name)
        {
	        NpcSound cache = CachedSound[name];
	        if (cache != null)
		        return cache;

	        if (Interface.Oxide.DataFileSystem.ExistsDatafile("IQSystem/IQReportSystem/Sounds/" + name))
	        {
		        NpcSound data = Interface.GetMod().DataFileSystem.ReadObject<NpcSound>("IQSystem/IQReportSystem/Sounds/" + name);
		        if (data == null)
			        return null;

		        CachedSound[name] = data;
		        return data;
	        }
	        else
	        {
		        PrintWarning(LanguageEn ? $"Could not find file named {name}. It should be located along the path: IQSystem/IQReportSystem/Sounds/{name}.json" : $"Не удалось найти файл с названием {name}. Он должен быть расположен по пути : IQSystem/IQReportSystem/Sounds/{name}.json");
		        return null;
	        }
        }

		
		
		public Boolean IsDuel(UInt64 userID)
		{
			if (!config.ReferenceSettings.NoCheckedDuel) return false;
			if (EventHelper)
			{
				if ((Boolean)EventHelper.CallHook("EMAtEvent", userID))
					return true;
			}

			if (Battles)
				return (Boolean)Battles?.Call("IsPlayerOnBattle", userID);
			if (Duel) return (Boolean)Duel?.Call("IsPlayerOnActiveDuel", BasePlayer.FindByID(userID));
			if (Duelist) return (Boolean)Duelist?.Call("inEvent", BasePlayer.FindByID(userID));
			if (ArenaTournament) return ArenaTournament.Call<Boolean>("IsOnTournament", userID);
			return false;
		}
		private Boolean IsFriends(UInt64 userID, UInt64 targetID)
		{
			if (!Friends)
				return RelationshipManager.ServerInstance.playerToTeam.ContainsKey(userID) && RelationshipManager.ServerInstance.playerToTeam[userID].members.Contains(targetID);
			return (Boolean)Friends?.Call("HasFriend", userID, targetID);
		}
		
		object OnItemCraft(ItemCraftTask task, BasePlayer player, Item item)
		{
			if (!PlayerChecks.ContainsKey(player.userID)) return null;

			if (config.CheckControllerSettings.TrackCrafting)
			{
				BasePlayer Moderator = BasePlayer.FindByID(PlayerChecks[player.userID].ModeratorID);
				if (Moderator != null)
					SendChat(GetLang("FUNCIONAL_MESSAGE_CHECK_AFK_PLAYER_START_CRAFTING", Moderator.UserIDString),
						Moderator);
			}

			player.ResetInputIdleTime();
			return null;
		}
        private class PlayerInformation
        {
	        public Int32 Reports;
	        public Int32 SendReports;
	        public Int32 AmountChecked;
	        public String LastModerator;

	        public List<Configuration.LangText> ReasonHistory = new List<Configuration.LangText>();
        }
        
        private void ConsoleCommandDiscord(ConsoleSystem.Arg arg)
        {
	        BasePlayer player = arg.Player();
	        if (player == null) return;
	        
	        String Discord = arg.HasArgs() ? String.Join(" ", arg.Args) : null;
	        SendPlayerDiscord(player, Discord);
        }
	    /// <summary>
	    /// Обновление 2.0.x
		/// - Добавлена поддержка нового формата Discord (с сохранением старого)
		/// - Перенес картинки UI с imgur на другой фото-обменник
	    /// </summary>
	    
	            
        private const Boolean LanguageEn = false;
		
		private void StopDamageRemove(UInt64 playerID)
		{
			if (StopDamageMan == null) return;
			if (!config.ReferenceSettings.StopDamageManSetting.UseStopDamage) return;
			
			StopDamageMan.CallHook("RemovePlayerSDM", playerID);
		}
		private void StartCheckRCC(UInt64 TargetID, UInt64 ModerID)
		{
			if (String.IsNullOrWhiteSpace(config.ReferenceSettings.RCCSettings.RCCKey)) return;
			String API = $"https://rustcheatcheck.ru/panel/api?action=addPlayer&key={config.ReferenceSettings.RCCSettings.RCCKey}&player={TargetID}";
			if (ModerID != 0)
				API += $"&moder={ModerID}";
			
			try
			{
				webrequest.Enqueue(API, null, (code, response) => { }, this);
			}
			catch (Exception e)
			{
				PrintError(LanguageEn ? "RCC : We were unable to get the player to check with RCC, please check if your key is up to date or if RCC is available" : "RCC : Мы не смогли вызвать игрока на проверку с помощью RCC, проверьте актуальность вашего ключа или доступность RCC");
			}
		}
		   		 		  						  	   		  	   		  	 				   		 		  				
		
		
		
		
		
		private void DrawUI_Report_Panel(BasePlayer player)
        {
	        String Interface = InterfaceBuilder.GetInterface($"{InterfaceBuilder.UI_LAYER}_BACKGORUND");
            if (Interface == null) return;
		   		 		  						  	   		  	   		  	 				   		 		  				
            Interface = Interface.Replace("%TITLE_PLAYER_HEADER_TITLE_SEND_REPORT%", GetLang("TITLE_PLAYER_HEADER_TITLE_SEND_REPORT", player.UserIDString));
            Interface = Interface.Replace("%TITLE_PLAYER_HEADER_TITLE_DESC_SEND_REPORT%", GetLang("TITLE_PLAYER_HEADER_TITLE_DESC_SEND_REPORT", player.UserIDString));
            Interface = Interface.Replace("%TITLE_PROFILE_INFO_CHECKED%", GetLang("TITLE_PROFILE_INFO_CHECKED", player.UserIDString, PlayerInformations[player.userID].AmountChecked));
            Interface = Interface.Replace("%NICK_PROFILE%", player.displayName.Length > 7 ? player.displayName.Substring(0, 7).ToUpper() + ".." : player.displayName.ToUpper());
            Interface = Interface.Replace("%AVATAR_PLAYER%", _.GetImage(player.UserIDString));
            Interface = Interface.Replace("%TITLE_NAME_REPORT_SYSTEM%", GetLang("TITLE_NAME_REPORT_SYSTEM", player.UserIDString));
            Interface = Interface.Replace("%TITLE_CLOSE_BUTTON_REPORT%", GetLang("TITLE_CLOSE_BUTTON_REPORT", player.UserIDString));

            CuiHelper.DestroyUi(player, InterfaceBuilder.UI_REPORT_PANEL);
            CuiHelper.AddUi(player, Interface);

            DrawUI_PlayerPanel(player);
            DrawUI_LeftMenu(player);
            DrawUI_HeaderUI_Search(player);
		   		 		  						  	   		  	   		  	 				   		 		  				
            if (IsModerator(player))
	            DrawUI_ModeratorStatistics(player);
        }		
        
        private IEnumerator SendSounds(BasePlayer player, String clipName, SpeakerEntityMgr.SpeakerEntity speakerEntity)
        {
	        NpcSound sound = LoadDataSound(clipName);
	        if (sound == null)
	        {
		        if (RoutineSounds[player] != null)
			        ServerMgr.Instance.StopCoroutine(RoutineSounds[player]);
		        
		        speakerEntity?.Kill();
		        yield break;
	        }
	        yield return CoroutineEx.waitForSeconds(0.1f);

	        foreach (byte[] data in sound.Data)
	        {
		        if (speakerEntity == null)
		        {
			        break;
		        }
		   		 		  						  	   		  		 			  			 		   					  		  
		        SendSound(speakerEntity.UID_SPEAKER, data);
		        yield return CoroutineEx.waitForSeconds(0.07f);
	        }
	        
	        if (RoutineSounds[player] != null)
		        ServerMgr.Instance.StopCoroutine(RoutineSounds[player]);
	        yield break;
        }

        
        
        private static StringBuilder sb = new StringBuilder();
        
                
        
				private List<Fields> DT_ChangeStatus(Boolean IsModerator, String PlayerName, String UserID, String StatusConnection)
		{
			List<Fields> fields;
			if (IsModerator)
			{
				fields = new List<Fields>
				{
					new Fields(LanguageEn ? "Moderator Information :" : "Информация о модераторе :", "", false),
					new Fields("", "", false),
					new Fields(LanguageEn ? "Nick" : "Ник", $"{PlayerName}", true),
					new Fields("Steam64ID", $"[{UserID}](https://steamcommunity.com/profiles/{UserID})", true),
					new Fields(LanguageEn ? "Status" : "Статус", $"{StatusConnection}", true),
				};
			}
			else
			{
				fields = new List<Fields>
				{
					new Fields(LanguageEn ? "Information about the suspect :" : "Информация о подозреваемом :", "", false),
					new Fields("", "", false),
					new Fields(LanguageEn ? "Nick" : "Ник", $"{PlayerName}", true),
					new Fields("Steam64ID", $"[{UserID}](https://steamcommunity.com/profiles/{UserID})", true),
					new Fields(LanguageEn ? "Status" : "Статус", $"{StatusConnection}", true),
				};
			}

			return fields;
		}
        private void AlertModerator(BasePlayer Moderator, String PlayerName, Int32 ReportCount)
        {
	        if (!config.ReportContollerModerationSettings.AlertModerationSettings.AlertModerator) return;
	        SendChat(GetLang("NOTIFY_MODERATOR_MAX_REPORT", Moderator.UserIDString, PlayerName, ReportCount), Moderator);
	        
	        RunEffect(Moderator);
        }

		
				public void StartSysncFakeActive() {
			if (!IQFakeActive) return;
			IQFakeActive?.Call("SyncReserved");
		}

        private readonly Regex _avatarRegex = new Regex(@"<avatarFull><!\[CDATA\[(.*)\]\]></avatarFull>", RegexOptions.Compiled);
		public class FakePlayer
		{
			public UInt64 UserID;
			public String DisplayName;
		}
                
        
        
		public class SpeakerEntityMgr
		{
			private static List<SpeakerEntity> _Entities = new List<SpeakerEntity>();
			public static SpeakerEntity Create(BasePlayer listeners)
			{
				var speaker = new SpeakerEntity();
				speaker.SetListeners(listeners);
				_Entities.Add(speaker);

				return speaker;
			}
			public static void Shutdown()
			{
				if (_Entities != null && _Entities.Count != 0)
					_Entities.ForEach(entity => _.NextTick(() => { entity?.Kill(); }));
			}
			public static void Kill(SpeakerEntity entity)
			{
				_Entities.Remove(entity);
			}
			
			public class SpeakerEntity
			{
				public UInt64 UID_SPEAKER = Network.Net.sv.TakeUID();
				private UInt64 UID_CHAIR = Network.Net.sv.TakeUID();
				public BasePlayer Listeners { get; private set; }
				public void SetListeners(BasePlayer listeners)
				{
					Listeners = listeners;
				}
				public void SendEntitities()
				{
					SendEntity(GetEntityChair);
					SendEntity(GetEntitySpeaker);
				}
				private ProtoBuf.Entity GetEntitySpeaker(BasePlayer player) =>
					new ProtoBuf.Entity()
					{
						baseNetworkable = new ProtoBuf.BaseNetworkable()
						{
							prefabID = player.prefabID,
							@group = BaseNetworkable.GlobalNetworkGroup.ID,
							uid = new NetworkableId(UID_SPEAKER),
						},
						baseEntity = new ProtoBuf.BaseEntity()
						{
							flags = 0,
							pos = new Vector3(0, 0, 0),
							rot = new Vector3(0, 0, 0),
							skinid = 0,
							time = Time.time,
						},
						baseCombat = new ProtoBuf.BaseCombat()
						{
							health = 10000,
							state = (Int32)BaseCombatEntity.LifeState.Alive
						},
						basePlayer = new ProtoBuf.BasePlayer()
						{
							health = 10000,
							modelState = new ModelState()
							{
								mounted = true,
								onground = true,
								ducked = true,
								prone = true
							},
							userid = UID_SPEAKER,
							name = "VOICE_P",
							playerFlags = (Int32)0,
							mounted = new NetworkableId(UID_CHAIR),
						},
						parent = new ProtoBuf.ParentInfo()
						{
							uid = new NetworkableId(UID_CHAIR),
							bone = 0
						}
					};

				private ProtoBuf.Entity GetEntityChair(BasePlayer player) =>
					new ProtoBuf.Entity()
					{
						baseNetworkable = new ProtoBuf.BaseNetworkable()
						{
							prefabID = 624857933, // static chair
							@group = BaseNetworkable.GlobalNetworkGroup.ID,
							uid = new NetworkableId(UID_CHAIR),
						},
						baseEntity = new ProtoBuf.BaseEntity()
						{
							flags = 0,
							pos = new Vector3(0, 0.5f, -2.3f),
							rot = new Vector3(90, 180, 180),
							skinid = 0,
							time = Time.time,
						},
						parent = new ProtoBuf.ParentInfo()
						{
							uid = player.net.ID,
							bone = 2822582055 // head
						}
					};
				
				public void Kill()
				{
					DestroyEntity(UID_SPEAKER);
					DestroyEntity(UID_CHAIR);

					SpeakerEntityMgr.Kill(this);
				}

				private void SendEntity(Func<BasePlayer, ProtoBuf.Entity> entityGetter)
				{
					NetWrite write = Network.Net.sv.StartWrite();

					write.PacketID(Message.Type.Entities);
					write.UInt32(++Listeners.net.connection.validate.entityUpdates);
					entityGetter(Listeners)?.WriteToStream(write);
					write.Send(new SendInfo(Listeners.net.connection));
				}

				private void DestroyEntity(UInt64 uid)
				{
					NetWrite write = Network.Net.sv.StartWrite();

					write.PacketID(Message.Type.EntityDestroy);
					write.UInt64(uid);
					write.Send(new SendInfo(Listeners.Connection) { priority = Priority.Immediate });
				}
			}
		}
        private readonly Hash<String, NpcSound> CachedSound = new Hash<String, NpcSound>();
        private List<Fields> DT_StartCheck(BasePlayer Target, BasePlayer Moderator, Boolean IsConsole = false)
        {
	        String ModeratorName = !IsConsole && Moderator != null ? Moderator.displayName : "Console";
	        String ModeratorID = !IsConsole && Moderator != null ? Moderator.UserIDString : "Console";
	        
	        List<Fields> fields = new List<Fields>
	        {
		        new Fields(LanguageEn ? "A new player check has been launched :" : "Запущена новая проверка игрока :", "", false),
		        new Fields("", "", false),
		        new Fields(LanguageEn ? "Moderator Information :" : "Информация о модераторе :", "", false),
		        new Fields("", "", false),
		        new Fields(LanguageEn ? "Nick" : "Ник", $"{ModeratorName}", true),
		        new Fields("Steam64ID", $"[{ModeratorID}](https://steamcommunity.com/profiles/{ModeratorID})", true),
		        new Fields("", "", false),
		        new Fields(LanguageEn ? "Information about the suspect :" : "Информация о подозреваемом :", "", false),
		        new Fields("", "", false),
		        new Fields(LanguageEn ? "Nick" : "Ник", $"{Target.displayName}", true),
		        new Fields("Steam64ID", $"[{Target.userID}](https://steamcommunity.com/profiles/{Target.userID})", true),
	        };

	        return fields;
        }
		

		
		
		private void OnPlayerReported(BasePlayer reporter, String targetName, String targetId, String subject, String message, String type)
		{
			if (!config.ReportF7AndGameMenuSettings.UseFunction) return;
			if (!type.Equals("cheat")) return;
			SendReportPlayer(reporter, UInt64.Parse(targetId), config.ReportF7AndGameMenuSettings.DefaultIndexReason);
		}
		private void DrawUI_Moderator_Button(BasePlayer moderator, String Command = "")
		{
			String Interface = InterfaceBuilder.GetInterface($"{InterfaceBuilder.UI_LAYER}_MODERATOR_MENU_CHECKED_BUTTON_VERDICT");
			if (Interface == null) return;
			
			Interface = Interface.Replace("%TITLE_PROFILE_MODERATOR_CHECKED_MENU_TITLE_BUTTON_RESULT%", GetLang("TITLE_PROFILE_MODERATOR_CHECKED_MENU_TITLE_BUTTON_RESULT", moderator.UserIDString));
			Interface = Interface.Replace("%COMMAND_VERDICT%", Command);

			CuiHelper.DestroyUi(moderator, "ButtonVerdictCheck");
			CuiHelper.AddUi(moderator, Interface);
		}		
		private void DrawUI_ShowPoopUP_Reason(BasePlayer player, String userID)
		{
			Int32 Y = 0;
			foreach (Configuration.ReasonReport reasonReport in config.ReasonList.Where(reasonReport => !reasonReport.HideUser).Take(4))
			{
				String Interface = InterfaceBuilder.GetInterface($"{InterfaceBuilder.UI_LAYER}_TEMPLATE_POOPUP_REASON");
				if (Interface == null) return;

				Interface = Interface.Replace("%OFFSET_MIN%", $"-154.66 {25 - (Y * 63)}");
				Interface = Interface.Replace("%OFFSET_MAX%", $"154.66 {83 - (Y * 63)}");
				Interface = Interface.Replace("%REASON%", reasonReport.Title.GetReasonTitle(player.userID));
				Interface = Interface.Replace("%POOPUP_REASON%", ImageUi.GetImage(config.ImagesSettings.PlayerListBlockSettings.PoopUpReasonBackgorund));
				Interface = Interface.Replace("%COMMAND%", $"report.panel send.player.report {userID} {Y}");
 
				CuiHelper.AddUi(player, Interface);

				Y++;
			}
		}
        
        private String VKT_PlayerSendContact(BasePlayer Sender, String Contact)
        {
	        String Message = LanguageEn
		        ? $"▣ PLAYER CONTACTS ▣\nInformation about the sender:\n• Nickname: {Sender.displayName}\n• Steam64ID: {Sender.userID} (https://steamcommunity.com/profiles/{Sender.userID})\nContact information: {Contact}"
		        : $"▣ КОНТАКТЫ ИГРОКА ▣\nИнформация об отправителе :\n• Ник : {Sender.displayName}\n• Steam64ID : {Sender.userID} (https://steamcommunity.com/profiles/{Sender.userID})\nКонтакты для связи : {Contact}";

	        return Message;
        }

        
        
        private void SendVerdictPlayer(UInt64 TargetID, BasePlayer Moderator, Configuration.ReasonReport Verdict)
        {
			Unsubscribe("OnPlayerBanned");
			Unsubscribe("OnPlayerDisconnected");
			
	        String VerdictReason = Verdict.Title.GetReasonTitle(TargetID);
	        
	        BasePlayer Target = BasePlayer.FindByID(TargetID);
	        if (Target != null)
	        {
		        if (config.VerdictControllerSettings.UseBanAllTeam)
		        {
			        if (Target.Team.members.Count >= 1)
			        {
				        if (config.ReasonList.Count >= config.VerdictControllerSettings.IndexBanReason)
				        {
					        Configuration.ReasonReport ReasonTeamBan = config.ReasonList[config.VerdictControllerSettings.IndexBanReason];
					        foreach (UInt64 TeamMembersIDS in Target.Team.members.Where(x => x != TargetID))
					        {
						        String VerdictReasonTeam = Verdict.Title.GetReasonTitle(TeamMembersIDS);
						        Server.Command(String.Format(ReasonTeamBan.BanCommand, TeamMembersIDS, VerdictReasonTeam));
						        BasePlayer TargetTeam = BasePlayer.FindByID(TeamMembersIDS);
						        if (TargetTeam != null)
							        TargetTeam.Kick(VerdictReasonTeam);
					        }
				        }
			        }
		        }
	        }
	        
	        Server.Command(String.Format(Verdict.BanCommand, TargetID, VerdictReason));
	        if (Target != null)
		        Target.Kick(VerdictReason);
	        
	        StopDamageRemove(TargetID);

	        BanPlayerRCC(TargetID, VerdictReason);
	        BanPlayerOzProtect(TargetID, Moderator == null ? 0 : Moderator.userID, VerdictReason);

	        if (Moderator != null)
	        {
		        SendChat(GetLang("FUNCIONAL_MODERATOR_VERDICT_RESULT", Moderator.UserIDString, Verdict.Title.GetReasonTitle(TargetID)), Moderator);
		        
		        CuiHelper.DestroyUi(Moderator, InterfaceBuilder.UI_REPORT_MODERATOR_MENU_CHECKED);
		        ModeratorInformations[Moderator.userID].AmountChecked++;
		        PlayerInformations[TargetID].LastModerator = Moderator.displayName;
	        }

	        if (config.NotifyChatSettings.UseNotifyVerdictCheck)
	        {
		        if (Moderator != null)
		        {
			        foreach (BasePlayer player in BasePlayer.activePlayerList)
				        SendChat(GetLang("NOTIFY_PLAYERS_STOP_CHECK_VERDICT_MODERATOR", player.UserIDString, Moderator.displayName, PlayerChecks[TargetID].DisplayName, VerdictReason), player);
		        }
		        else
		        {
			        foreach (BasePlayer player in BasePlayer.activePlayerList)
				        SendChat(GetLang("NOTIFY_PLAYERS_STOP_CHECK_NOT_MODERATOR", player.UserIDString, PlayerChecks[TargetID].DisplayName, VerdictReason), player);
		        }
	        }

	        Configuration.NotifyDiscord.Webhooks.TemplatesNotify TemplateDiscord = config.NotifyDiscordSettings.WebhooksList.NotifyStopCheck;
			
	        if (!String.IsNullOrWhiteSpace(TemplateDiscord.WebhookNotify))
	        {
		        List<Fields> fields = DT_StopCheck(TargetID, Moderator, Verdict: Verdict);
		        SendDiscord(TemplateDiscord.WebhookNotify, fields, GetAuthorDiscord(TemplateDiscord), TemplateDiscord.Color);
	        }
	        
	        SendVK(VKT_StopCheck(TargetID, Moderator, Verdict: Verdict));
	        
	        if (PlayerChecks.ContainsKey(TargetID))
		        PlayerChecks.Remove(TargetID);
	        
	        Interface.Call("OnVerdictChecked", TargetID, Moderator, VerdictReason, Verdict.BanCommand);
	        
	        Subscribe("OnPlayerBanned");
	        Subscribe("OnPlayerDisconnected");
        }

		
				
		private Boolean IsSteam(String id)
		{
			if (MultiFighting == null) return true;
			
			BasePlayer player = BasePlayer.Find(id);
			if (player == null)
				return false;

			Object obj = MultiFighting.CallHook("IsSteam", player.Connection);
			if (obj is Boolean)
				return (Boolean)obj;
				
			return false;
		}
		   		 		  						  	   		  	   		  	 				   		 		  				
        
        
        private void AlertMaxReportDiscord(String PlayerName, Int32 ReportCount, UInt64 TargetID)
        {
	        Configuration.NotifyDiscord.Webhooks.TemplatesNotify TemplateDiscord = config.NotifyDiscordSettings.WebhooksList.NotifyMaxReport;
			
	        if (!String.IsNullOrWhiteSpace(TemplateDiscord.WebhookNotify))
	        {
		        List<Fields> fields = DT_PlayerMaxReport(PlayerName, ReportCount, TargetID);
		        SendDiscord(TemplateDiscord.WebhookNotify, fields, GetAuthorDiscord(TemplateDiscord), TemplateDiscord.Color);
	        }
	        
	        SendVK(VKT_PlayerMaxReport(PlayerName, ReportCount, TargetID));
        }
        private Dictionary<UInt64, ModeratorInformation> ModeratorInformations = new Dictionary<UInt64, ModeratorInformation>();
		
		
		public List<FakePlayer> PlayerBases = new List<FakePlayer>();

			}
}