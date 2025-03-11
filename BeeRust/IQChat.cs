using Newtonsoft.Json.Linq;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;
using UnityEngine.Networking;
using Newtonsoft.Json;
using System;
using Oxide.Core.Plugins;
using Oxide.Core;
using Oxide.Core.Libraries;
using Facepunch;
using System.Linq;
using System.Text;
using CompanionServer;
using ConVar;
using System.Collections;
using Oxide.Game.Rust.Cui;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("IQChat", "Mercury", "2.25.7")]
    [Description("The most pleasant chat for your server from the IQ system")]
    class IQChat : RustPlugin
    {
		   		 		  						  	   		  	  			  	  			  	 				  	  	
        
        
        private bool OnPlayerChat(BasePlayer player, string message, Chat.ChatChannel channel)
        {
            // PrintToChat(message);
            if (Interface.Oxide.CallHook("CanChatMessage", player, message) != null) return false;
            SeparatorChat(channel, player, message);
            return false;
        }
        
        private class InterfaceBuilder
        {
            
            public static InterfaceBuilder Instance;
            public const String UI_Chat_Context = "UI_IQCHAT_CONTEXT";
            public const String UI_Chat_Context_Visual_Nick = "UI_IQCHAT_CONTEXT_VISUAL_NICK";
            public const String UI_Chat_Alert = "UI_IQCHAT_ALERT";
            public Dictionary<String, String> Interfaces;

            
            
            public InterfaceBuilder()
            {
                Instance = this;
                Interfaces = new Dictionary<String, String>();
                BuildingStaticContext();
                BuildingVisualNick();
                BuildingCheckBox();

                BuildingModerationStatic();
                BuildingMuteAllChat();
                BuildingMuteAllVoice();

                BuildingSlider();
                BuildingSliderUpdateArgument();
		   		 		  						  	   		  	  			  	  			  	 				  	  	
                BuildingDropList();
                BuildingOpenDropList();
                BuildingElementDropList();
                BuildingElementDropListTakeLine();

                BuildingAlertUI();

                BuildingMuteAndIgnore();
                BuildingMuteAndIgnorePlayerPanel();
                BuildingMuteAndIgnorePlayer();
                BuildingMuteAndIgnorePages();

                BuildingMuteAndIgnorePanelAlert();
                BuildingIgnoreAlert();
                BuildingMuteAlert();
                BuildingMuteAlert_DropList_Title();
                BuildingMuteAlert_DropList_Reason();
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

            public static string GetInterface(String name)
            {
                string json = string.Empty;
                if (Instance.Interfaces.TryGetValue(name, out json) == false)
                {
                    _.PrintWarning($"Warning! UI elements not found by name! -> {name}");
                }

                return json;
            }

            public static void DestroyAll()
            {
                for (var i = 0; i < BasePlayer.activePlayerList.Count; i++)
                {
                    var player = BasePlayer.activePlayerList[i];
                    CuiHelper.DestroyUi(player, UI_Chat_Context);
                    CuiHelper.DestroyUi(player, UI_Chat_Context_Visual_Nick);
                    CuiHelper.DestroyUi(player, UI_Chat_Alert);
                    CuiHelper.DestroyUi(player, "MUTE_AND_IGNORE_PANEL_ALERT");
                }
            }

            
            
                        private void BuildingVisualNick()
            {
                CuiElementContainer container = new CuiElementContainer();

                container.Add(new CuiElement
                {
                    Name = UI_Chat_Context_Visual_Nick,
                    Parent = UI_Chat_Context,
                    Components = {
                    new CuiTextComponent { Text = "%NICK_DISPLAY%", Font = "robotocondensed-regular.ttf", FontSize = 7, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-135.769 -89.558", OffsetMax = "-12.644 -77.176" }
                }
                });

                AddInterface("UI_Chat_Context_Visual_Nick", container.ToJson());
            }
            
                        private void BuildingStaticContext()
            {
                Configuration.ControllerParameters Controller = config.ControllerParameter;
                if (Controller == null)
                {
                    _.PrintWarning("Ошибка генерации интерфейса, null значение в конфигурации, свяжитесь с разработчиком");
                    return;
                }
                CuiElementContainer container = new CuiElementContainer();

                container.Add(new CuiPanel
                {
                    CursorEnabled = true,
                    RectTransform = { AnchorMin = "1 0.5", AnchorMax = "1 0.5", OffsetMin = "-379 -217", OffsetMax = "-31 217" },
                    Image = { Color = "0 0 0 0" }
                }, "Overlay", UI_Chat_Context);
		   		 		  						  	   		  	  			  	  			  	 				  	  	
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "1000 1000", OffsetMax = "-1000 -1000" },
                    Button = { Close = UI_Chat_Context, Color = "0 0 0 0.5" },
                    Text = { Text = "" }
                }, UI_Chat_Context, "CLOSE_UI_Chat_Context_FullScreen");

                container.Add(new CuiElement
                {
                    Name = "ImageContext",
                    Parent = UI_Chat_Context,
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = "%IMG_BACKGROUND%" },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "TitleLabel",
                    Parent = UI_Chat_Context,
                    Components = {
                    new CuiTextComponent { Text = "%TITLE%", Font = "robotocondensed-regular.ttf", FontSize = 17, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-149.429 166.408", OffsetMax = "-14.788 189.564" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "DescriptionLabel",
                    Parent = UI_Chat_Context,
                    Components = {
                    new CuiTextComponent { Text = "%SETTING_ELEMENT%", Font = "robotocondensed-regular.ttf", FontSize = 8, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-149.429 112.0214442", OffsetMax = "152.881 131.787" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "InformationLabel",
                    Parent = UI_Chat_Context,
                    Components = {
                    new CuiTextComponent { Text = "%INFORMATION%", Font = "robotocondensed-regular.ttf", FontSize = 8, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-149.429 -53.432", OffsetMax = "-32.905 -39.808" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "InformationIcon",
                    Parent = UI_Chat_Context,
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage("IQCHAT_INFORMATION_ICON") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-14.788 -52.12", OffsetMax = "-3.788 -41.12" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "SettingLabel",
                    Parent = UI_Chat_Context,
                    Components = {
                    new CuiTextComponent { Text = "%SETTINGS%", Font = "robotocondensed-regular.ttf", FontSize = 8, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "11.075 -53.432", OffsetMax = "126.125 -39.808" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "SettingIcon",
                    Parent = UI_Chat_Context,
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage("IQCHAT_SETTING_ICON") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "141.88 -52.12", OffsetMax = "152.88 -41.12" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "SettingPM",
                    Parent = UI_Chat_Context,
                    Components = {
                    new CuiTextComponent { Text = "%SETTINGS_PM%", Font = "robotocondensed-regular.ttf", FontSize = 8, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "11.075 -70.712", OffsetMax = "126.125 -57.088" }
                }
                });
		   		 		  						  	   		  	  			  	  			  	 				  	  	
                container.Add(new CuiElement
                {
                    Name = "SettingAlertChat",
                    Parent = UI_Chat_Context,
                    Components = {
                    new CuiTextComponent { Text = "%SETTINGS_ALERT%", Font = "robotocondensed-regular.ttf", FontSize = 8, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "11.075 -82.412", OffsetMax = "126.125 -68.788" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "SettingNoticyChat",
                    Parent = UI_Chat_Context,
                    Components = {
                    new CuiTextComponent { Text = "%SETTINGS_ALERT_PM%", Font = "robotocondensed-regular.ttf", FontSize = 8, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "11.075 -94.412", OffsetMax = "126.125 -80.788" }
                }
                });
		   		 		  						  	   		  	  			  	  			  	 				  	  	
                container.Add(new CuiElement
                {
                    Name = "SettingSoundAlert",
                    Parent = UI_Chat_Context,
                    Components = {
                    new CuiTextComponent { Text = "%SETTINGS_SOUNDS%", Font = "robotocondensed-regular.ttf", FontSize = 8, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "11.075 -106.412", OffsetMax = "126.125 -92.788" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "MuteStatus",
                    Parent = UI_Chat_Context,
                    Components = {
                    new CuiTextComponent { Text = "%MUTE_STATUS_PLAYER%", Font = "robotocondensed-regular.ttf", FontSize = 11, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-143.174 -131.59", OffsetMax = "-120.611 -114.967" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "MuteStatusTitle",
                    Parent = UI_Chat_Context,
                    Components = {
                    new CuiTextComponent { Text = "%MUTE_STATUS_TITLE%", Font = "robotocondensed-regular.ttf", FontSize = 7, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-143.174 -141.429", OffsetMax = "-89.127 -132.508" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "CountIgnored",
                    Parent = UI_Chat_Context,
                    Components = {
                    new CuiTextComponent { Text = "%IGNORED_STATUS_COUNT%", Font = "robotocondensed-regular.ttf", FontSize = 7, Align = TextAnchor.UpperLeft, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-66.98 -131.715", OffsetMax = "-11.09 -116.831" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "IgonoredTitle",
                    Parent = UI_Chat_Context,
                    Components = {
                    new CuiTextComponent { Text = "%IGNORED_STATUS_TITLE%", Font = "robotocondensed-regular.ttf", FontSize = 7, Align = TextAnchor.UpperLeft, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-66.98 -142.04", OffsetMax = "-19.967 -132.537" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "IgnoredIcon",
                    Parent = UI_Chat_Context,
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage("IQCHAT_IGNORE_INFO_ICON") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-19.483 -115.225", OffsetMax = "-11.762 -107.814" }
                }
                });

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Command = $"newui.cmd action.mute.ignore open {SelectedAction.Ignore}", Color = "0 0 0 0" },
                    Text = { Text = "" }
                }, "IgnoredIcon", "CLOSE_IGNORED");

                container.Add(new CuiElement
                {
                    Name = "TitleNickPanel",
                    Parent = UI_Chat_Context,
                    Components = {
                    new CuiTextComponent { Text = "%NICK_DISPLAY_TITLE%", Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-135.769 -78.878", OffsetMax = "-85.632 -64.613" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "NickTitle",
                    Parent = UI_Chat_Context,
                    Components = {
                    new CuiTextComponent { Text = "%SLIDER_NICK_COLOR_TITLE%", Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "22.591 76.362", OffsetMax = "80.629 92.278" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "ChatMessageTitle",
                    Parent = UI_Chat_Context,
                    Components = {
                    new CuiTextComponent { Text = "%SLIDER_MESSAGE_COLOR_TITLE%", Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-136.591 9.362", OffsetMax = "-78.045 24.278" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "PrefixTitle",
                    Parent = UI_Chat_Context,
                    Components = {
                    new CuiTextComponent { Text = "%SLIDER_PREFIX_TITLE%", Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-136.591 77.362", OffsetMax = "-89.949 93.278" }
                }
                });


                container.Add(new CuiElement
                {
                    Name = "RankTitle",
                    Parent = UI_Chat_Context,
                    Components = {
                        new CuiTextComponent { Text = "%SLIDER_IQRANK_TITLE%", Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "22.825 9.242", OffsetMax = "81.375 25.158" }
                    }
                });

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "123.62 166", OffsetMax = "153.62 196" },
                    Button = { Close = UI_Chat_Context, Color = "0 0 0 0" },
                    Text = { Text = "" }
                }, UI_Chat_Context, "CLOSE_UI_Chat_Context");

                AddInterface("UI_Chat_Context", container.ToJson());
            }

            
                        private void BuildingCheckBox()
            {
                CuiElementContainer container = new CuiElementContainer();

                container.Add(new CuiElement
                {
                    Name = "%NAME_CHECK_BOX%",
                    Parent = UI_Chat_Context,
                    Components = {
                    new CuiRawImageComponent { Color = "%COLOR%", Png = ImageUi.GetImage("IQCHAT_ELEMENT_SETTING_CHECK_BOX") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "%OFFSET_MIN%", OffsetMax = "%OFFSET_MAX%" }
                }
                });

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Command = "%COMMAND_TURNED%", Color = "0 0 0 0" },
                    Text = { Text = "" }
                }, "%NAME_CHECK_BOX%", "CHECK_BOX_TURNED");

                AddInterface("UI_Chat_Context_CheckBox", container.ToJson());
            }
            
                        private void BuildingSlider()
            {
                CuiElementContainer container = new CuiElementContainer();
                String NameSlider = "%NAME%";

                container.Add(new CuiElement
                {
                    Name = NameSlider,
                    Parent = UI_Chat_Context,
                    Components = {
                            new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage("IQCHAT_ELEMENT_SLIDER_ICON") },
                            new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "%OFFSET_MIN%" , OffsetMax = "%OFFSET_MAX%"  }
                        }
                });
		   		 		  						  	   		  	  			  	  			  	 				  	  	
                container.Add(new CuiElement
                {
                    Name = "Left",
                    Parent = NameSlider,
                    Components = {
                        new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage("IQCHAT_ELEMENT_SLIDER_LEFT_ICON") },
                        new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-53.9 -4.5", OffsetMax = "-48.9 4.5" }
                    }
                });
		   		 		  						  	   		  	  			  	  			  	 				  	  	
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Command = "%COMMAND_LEFT_SLIDE%", Color = "0 0 0 0" },
                    Text = { Text = "" }
                }, "Left", "LEFT_SLIDER_BTN");
		   		 		  						  	   		  	  			  	  			  	 				  	  	
                container.Add(new CuiElement
                {
                    Name = "Right",
                    Parent = NameSlider,
                    Components = {
                        new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage("IQCHAT_ELEMENT_SLIDER_RIGHT_ICON") },
                        new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "48.92 -4.5", OffsetMax = "53.92 4.5" }
                    }
                });

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Command = "%COMMAND_RIGHT_SLIDE%", Color = "0 0 0 0" },
                    Text = { Text = "" }
                }, "Right", "RIGHT_SLIDER_BTN");

                AddInterface("UI_Chat_Slider", container.ToJson());
            }
            private void BuildingSliderUpdateArgument()
            {
                CuiElementContainer container = new CuiElementContainer();
                String ParentSlider = "%PARENT%";
                String NameArgument = "%NAME%";

                container.Add(new CuiElement
                {
                    Name = NameArgument,
                    Parent = ParentSlider,
                    Components = {
                    new CuiTextComponent { Text = "%ARGUMENT%", Font = "robotocondensed-regular.ttf", FontSize = 9, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-41.929 -6.801", OffsetMax = "41.929 6.801" }
                }
                });

                AddInterface("UI_Chat_Slider_Update_Argument", container.ToJson());
            }
            
            
                        private void BuildingMuteAndIgnore()
            {
                CuiElementContainer container = new CuiElementContainer();

                container.Add(new CuiElement
                {
                    Name = "MuteAndIgnoredPanel",
                    Parent = UI_Chat_Context,
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage("IQCHAT_MUTE_AND_IGNORE_PANEL")},
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-1007.864 -220.114", OffsetMax = "-167.374 219.063" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "TitlesPanel",
                    Parent = "MuteAndIgnoredPanel",
                    Components = {
                    new CuiTextComponent { Text = "%TITLE%", Font = "robotocondensed-regular.ttf", FontSize = 20, Align = TextAnchor.MiddleRight, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "60.217 164.031", OffsetMax = "356.114 190.962" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "IconPanel",
                    Parent = "MuteAndIgnoredPanel",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage("IQCHAT_MUTE_AND_IGNORE_ICON")},
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "357.5 170", OffsetMax = "373.5 185"  }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "SearchPanel",
                    Parent = "MuteAndIgnoredPanel",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage("IQCHAT_MUTE_AND_IGNORE_SEARCH")},
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-385.8 161.244", OffsetMax = "-186.349 192.58" }
                }
                });

                string SearchName = "";

                container.Add(new CuiElement
                {
                    Parent = "SearchPanel",
                    Name = "SearchPanel" + ".Input.Current",
                    Components =
                {
                    new CuiInputFieldComponent { Text = SearchName, FontSize = 14,Command = $"newui.cmd action.mute.ignore search.controller %ACTION_TYPE% {SearchName}", Align = TextAnchor.MiddleCenter, Color = "1 1 1 0.5", CharsLimit = 15},
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "PanelPages",
                    Parent = "MuteAndIgnoredPanel",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage("IQCHAT_MUTE_AND_IGNORE_PAGE_PANEL")},
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-179.196 161.242", OffsetMax = "-121.119 192.578" }
                }
                });

                AddInterface("UI_Chat_Mute_And_Ignore", container.ToJson());
            }

            private void BuildingMuteAndIgnorePlayerPanel()
            {
                CuiElementContainer container = new CuiElementContainer();

                container.Add(new CuiPanel
                {
                    CursorEnabled = true,
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0.85" },
                    Image = { Color = "0 0 0 0" }
                }, "MuteAndIgnoredPanel", "MuteIgnorePanelContent");

                AddInterface("UI_Chat_Mute_And_Ignore_Panel_Content", container.ToJson());
            }
            private void BuildingMuteAndIgnorePlayer()
            {
                CuiElementContainer container = new CuiElementContainer();

                container.Add(new CuiElement
                {
                    Name = "PANEL_PLAYER",
                    Parent = "MuteIgnorePanelContent",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage("IQCHAT_MUTE_AND_IGNORE_PLAYER") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "%OFFSET_MIN%", OffsetMax = "%OFFSET_MAX%" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "NickName",
                    Parent = "PANEL_PLAYER",
                    Components = {
                    new CuiTextComponent { Text = "%DISPLAY_NAME%", Font = "robotocondensed-regular.ttf", FontSize = 12, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-77.391 -17.245", OffsetMax = "91.582 17.244" }
                }
                });
		   		 		  						  	   		  	  			  	  			  	 				  	  	
                container.Add(new CuiElement
                {
                    Name = "StatusPanel",
                    Parent = "PANEL_PLAYER",
                    Components = {
                    new CuiRawImageComponent { Color = "%COLOR%", Png = ImageUi.GetImage("IQCHAT_MUTE_AND_IGNORE_PLAYER_STATUS") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-92.231 -11.655", OffsetMax = "-87.503 10.44" }
                }
                });
		   		 		  						  	   		  	  			  	  			  	 				  	  	
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Command = "%COMMAND_ACTION%", Color = "0 0 0 0" },
                    Text = { Text = "" }
                }, "PANEL_PLAYER");

                AddInterface("UI_Chat_Mute_And_Ignore_Player", container.ToJson());
            }
            private void BuildingMuteAndIgnorePages()
            {
                CuiElementContainer container = new CuiElementContainer();

                container.Add(new CuiElement
                {
                    Name = "PageCount",
                    Parent = "PanelPages",
                    Components = {
                    new CuiTextComponent { Text = "%PAGE%", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-11.03 -15.668", OffsetMax = "11.03 15.668" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "LeftPage",
                    Parent = "PanelPages",
                    Components = {
                    new CuiRawImageComponent { Color = "%COLOR_LEFT%", Png = ImageUi.GetImage("IQCHAT_ELEMENT_SLIDER_LEFT_ICON") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-18 -7", OffsetMax = "-13 6" }
                }
                });

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Command = "%COMMAND_LEFT%", Color = "0 0 0 0" },
                    Text = { Text = "" }
                }, "LeftPage");

                container.Add(new CuiElement
                {
                    Name = "RightPage",
                    Parent = "PanelPages",
                    Components = {
                    new CuiRawImageComponent { Color = "%COLOR_RIGHT%", Png = ImageUi.GetImage("IQCHAT_ELEMENT_SLIDER_RIGHT_ICON") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "14 -7", OffsetMax = "19 6" }
                }
                });

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Command = "%COMMAND_RIGHT%", Color = "0 0 0 0" },
                    Text = { Text = "" }
                }, "RightPage");

                AddInterface("UI_Chat_Mute_And_Ignore_Pages", container.ToJson());
            }

            
                        private void BuildingMuteAndIgnorePanelAlert()
            {
                CuiElementContainer container = new CuiElementContainer();

                container.Add(new CuiPanel
                {
                    CursorEnabled = true,
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Image = { Color = "0 0 0 0.25", Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" }
                }, "Overlay", "MUTE_AND_IGNORE_PANEL_ALERT");

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Close = "MUTE_AND_IGNORE_PANEL_ALERT", Color = "0 0 0 0" },
                    Text = { Text = "" }
                }, "MUTE_AND_IGNORE_PANEL_ALERT");

                AddInterface("UI_Chat_Mute_And_Ignore_Alert_Panel", container.ToJson());
            }

            
            private void BuildingMuteAlert()
            {
                CuiElementContainer container = new CuiElementContainer();

                container.Add(new CuiElement
                {
                    Name = "AlertMute",
                    Parent = "MUTE_AND_IGNORE_PANEL_ALERT",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage("IQCHAT_MUTE_ALERT_PANEL") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-199.832 -274.669", OffsetMax = "199.832 274.669" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "AlertMuteIcon",
                    Parent = "AlertMute",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage("IQCHAT_MUTE_ALERT_ICON") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-67 204.8", OffsetMax = "67 339.8" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "AlertMuteTitles",
                    Parent = "AlertMute",
                    Components = {
                    new CuiTextComponent { Text = "%TITLE%", Font = "robotocondensed-regular.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-199.828 142.57", OffsetMax = "199.832 179.43" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "AlertMuteTakeChat",
                    Parent = "AlertMute",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1",Png = ImageUi.GetImage("IQCHAT_IGNORE_ALERT_BUTTON_YES") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-99.998 87.944", OffsetMax = "100.002 117.944" }
                }
                });
		   		 		  						  	   		  	  			  	  			  	 				  	  	
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Command = "%COMMAND_TAKE_ACTION_MUTE_CHAT%", Color = "0 0 0 0" },
                    Text = { Text = "%BUTTON_TAKE_CHAT_ACTION%", Align = TextAnchor.MiddleCenter, FontSize = 18, Color = "0.1294118 0.145098 0.1647059 1" }
                }, "AlertMuteTakeChat", "BUTTON_TAKE_CHAT");

                container.Add(new CuiElement
                {
                    Name = "AlertMuteTakeVoice",
                    Parent = "AlertMute",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1",Png = ImageUi.GetImage("IQCHAT_IGNORE_ALERT_BUTTON_YES") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-100 49.70", OffsetMax = "100 79.70" } //
                }
                });

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Command = "%COMMAND_TAKE_ACTION_MUTE_VOICE%", Color = "0 0 0 0" },
                    Text = { Text = "%BUTTON_TAKE_VOICE_ACTION%", Align = TextAnchor.MiddleCenter, FontSize = 18, Color = "0.1294118 0.145098 0.1647059 1" }
                }, "AlertMuteTakeVoice", "BUTTON_TAKE_VOICE");
		   		 		  						  	   		  	  			  	  			  	 				  	  	
                AddInterface("UI_Chat_Mute_Alert", container.ToJson());
            }
            private void BuildingMuteAlert_DropList_Title()
            {
                CuiElementContainer container = new CuiElementContainer();

                container.Add(new CuiElement
                {
                    Name = "AlertMuteTitleReason",
                    Parent = "AlertMute",
                    Components = {
                    new CuiTextComponent { Text = "%TITLE%", Font = "robotocondensed-regular.ttf", FontSize = 22, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-199.828 -9.430", OffsetMax = "199.832 27.430" }
                }
                });

                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "1 1 1 0" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-147.497 -265.5440", OffsetMax = "147.503 -24.70" }
                }, "AlertMute", "PanelMuteReason");
		   		 		  						  	   		  	  			  	  			  	 				  	  	
                AddInterface("UI_Chat_Mute_Alert_DropList_Title", container.ToJson());
            }

            private void BuildingMuteAlert_DropList_Reason()
            {
                CuiElementContainer container = new CuiElementContainer();

                container.Add(new CuiElement
                {
                    Name = "Reason",
                    Parent = "PanelMuteReason",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage("IQCHAT_MUTE_ALERT_PANEL_REASON")},
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "%OFFSET_MIN%", OffsetMax = "%OFFSET_MAX%" }
                }
                });

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Command = "%COMMAND_REASON%", Color = "0 0 0 0" },
                    Text = { Text = "%REASON%", Align = TextAnchor.MiddleCenter, FontSize = 13, Color = "1 1 1 1" }
                }, "Reason");

                AddInterface("UI_Chat_Mute_Alert_DropList_Reason", container.ToJson());
            }
            
                        private void BuildingIgnoreAlert()
            {
                CuiElementContainer container = new CuiElementContainer();

                container.Add(new CuiElement
                {
                    Name = "AlertIgnore",
                    Parent = "MUTE_AND_IGNORE_PANEL_ALERT",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage("IQCHAT_IGNORE_ALERT_PANEL") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-236.5 -134", OffsetMax = "236.5 134" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "AlertIgnoreIcon",
                    Parent = "AlertIgnore",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage("IQCHAT_IGNORE_ALERT_ICON") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-66.5 64.8", OffsetMax = "66.5 198.8" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "AlertIgnoreTitle",
                    Parent = "AlertIgnore",
                    Components = {
                    new CuiTextComponent { Text = "%TITLE%", Font = "robotocondensed-regular.ttf", FontSize = 22, Align = TextAnchor.UpperCenter, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-231 -55.00", OffsetMax = "229.421 33.98" } //
                }
                });

                container.Add(new CuiElement
                {
                    Name = "AlertIgnoreYes",
                    Parent = "AlertIgnore",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage("IQCHAT_IGNORE_ALERT_BUTTON_YES") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-178 -115", OffsetMax = "-22 -77" }
                }
                });

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Close = "MUTE_AND_IGNORE_PANEL_ALERT", Command = "%COMMAND%", Color = "0 0 0 0" },
                    Text = { Text = "%BUTTON_YES%", Align = TextAnchor.MiddleCenter, FontSize = 18, Color = "0.1294118 0.145098 0.1647059 1" }
                }, "AlertIgnoreYes", "BUTTON_YES");

                container.Add(new CuiElement
                {
                    Name = "AlertIgnoreNo",
                    Parent = "AlertIgnore",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage("IQCHAT_IGNORE_ALERT_BUTTON_NO") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "22 -115", OffsetMax = "178 -77" }
                }
                });

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Close = "MUTE_AND_IGNORE_PANEL_ALERT", Color = "0 0 0 0" },
                    Text = { Text = "%BUTTON_NO%", Align = TextAnchor.MiddleCenter, FontSize = 18 }
                }, "AlertIgnoreNo", "BUTTON_NO");

                AddInterface("UI_Chat_Ignore_Alert", container.ToJson());
            }
            
            
            
            
            private void BuildingDropList()
            {
                CuiElementContainer container = new CuiElementContainer();

                container.Add(new CuiElement
                {
                    Name = "DropListIcon",
                    Parent = UI_Chat_Context,
                    Components = {
                            new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage("IQCHAT_ELEMENT_PREFIX_MULTI_TAKE_ICON")},
                      new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "%OFFSET_MIN%", OffsetMax = "%OFFSET_MAX%" }
                        }
                });

                container.Add(new CuiElement
                {
                    Name = "DropListDescription",
                    Parent = "DropListIcon",
                    Components = {
                            new CuiTextComponent { Text = "%TITLE%", Font = "robotocondensed-regular.ttf", FontSize = 8, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                            new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-105.5 -13.948", OffsetMax = "-42.615 1.725" }
                        }
                });

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Command = "%BUTTON_DROP_LIST_CMD%", Color = "0 0 0 0" },
                    Text = { Text = "" }
                }, "DropListIcon", "DropListIcon_Button");

                AddInterface("UI_Chat_DropList", container.ToJson());
            }

            private void BuildingOpenDropList()
            {
                CuiElementContainer container = new CuiElementContainer();

                container.Add(new CuiElement
                {
                    Name = "OpenDropList",
                    Parent = UI_Chat_Context,
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage("IQCHAT_ELEMENT_DROP_LIST_OPEN_ICON")},
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-149.429 -17.38", OffsetMax = "155.093 109.1" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "DropListName",
                    Parent = "OpenDropList",
                    Components = {
                    new CuiTextComponent { Text = "%TITLE%", Font = "robotocondensed-regular.ttf", FontSize = 11, Align = TextAnchor.UpperLeft, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-140.329 44.5", OffsetMax = "-40.329 58.312" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "DropListDescription",
                    Parent = "OpenDropList",
                    Components = {
                    new CuiTextComponent { Text = "%DESCRIPTION%", Font = "robotocondensed-regular.ttf", FontSize = 8, Align = TextAnchor.UpperLeft, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-140.329 32.993", OffsetMax = "-40.329 42.77" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "DropListClose",
                    Parent = "OpenDropList",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage("IQCHAT_ELEMENT_PREFIX_MULTI_TAKE_ICON") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "113 32.2", OffsetMax = "145 56.2" }
                }
                });

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Close = "OpenDropList", Color = "0 0 0 0" },
                    Text = { Text = "" }
                }, "DropListClose", "DropListClose_Button");

                container.Add(new CuiElement
                {
                    Name = "DropListPageRight",
                    Parent = "OpenDropList",
                    Components = {
                    new CuiRawImageComponent { Color = "%COLOR_RIGHT%", Png = ImageUi.GetImage("IQCHAT_ELEMENT_SLIDER_RIGHT_ICON") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "100 38", OffsetMax = "105.2 48" }
                }
                });

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Command = "%NEXT_BTN%", Color = "0 0 0 0" },
                    Text = { Text = "" }
                }, "DropListPageRight", "DropListPageRight_Button");

                container.Add(new CuiElement
                {
                    Name = "DropListPageLeft",
                    Parent = "OpenDropList",
                    Components = {
                    new CuiRawImageComponent { Color ="%COLOR_LEFT%", Png = ImageUi.GetImage("IQCHAT_ELEMENT_SLIDER_LEFT_ICON") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "86 38", OffsetMax = "91.2 48" }
                }
                });

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Command = "%BACK_BTN%", Color = "0 0 0 0" },
                    Text = { Text = "" }
                }, "DropListPageLeft", "DropListPageLeft_Button");

                AddInterface("UI_Chat_OpenDropList", container.ToJson());
            }

            private void BuildingElementDropList()
            {
                CuiElementContainer container = new CuiElementContainer();
                String Name = "ArgumentDropList_%COUNT%";

                container.Add(new CuiElement
                {
                    Name = Name,
                    Parent = "OpenDropList",
                    Components = {
                    new CuiRawImageComponent { FadeIn = 0.3f, Color = "1 1 1 1", Png = ImageUi.GetImage("IQCHAT_ELEMENT_DROP_LIST_OPEN_ARGUMENT_ICON") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "%OFFSET_MIN%", OffsetMax = "%OFFSET_MAX%" }
                }
                });

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-37.529 -12.843", OffsetMax = "37.528 12.842" },
                    Button = { FadeIn = 0.3f, Command = "%TAKE_COMMAND_ARGUMENT%", Color = "0 0 0 0" },
                    Text = { FadeIn = 0.3f, Text = "%ARGUMENT%", Font = "robotocondensed-regular.ttf", FontSize = 9, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" }
                }, Name, "ArgumentButton");

                AddInterface("UI_Chat_OpenDropListArgument", container.ToJson());
            }

            private void BuildingElementDropListTakeLine()
            {
                CuiElementContainer container = new CuiElementContainer();
                String Parent = "ArgumentDropList_%COUNT%";

                container.Add(new CuiElement
                {
                    Name = "TAKED_INFO_%COUNT%",
                    Parent = Parent,
                    Components = {
                    new CuiRawImageComponent { Color = "0.3098039 0.2745098 0.572549 1", Png = ImageUi.GetImage("IQCHAT_ELEMENT_DROP_LIST_OPEN_TAKED") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-25.404 -17.357", OffsetMax = "25.403 -1.584" }
                }
                });

                AddInterface("UI_Chat_OpenDropListArgument_Taked", container.ToJson());
            }

            
                        private void BuildingModerationStatic()
            {
                CuiElementContainer container = new CuiElementContainer();

                container.Add(new CuiElement
                {
                    Name = "ModerationLabel",
                    Parent = UI_Chat_Context,
                    Components = {
                    new CuiTextComponent { Text = "%TITLE%", Font = "robotocondensed-regular.ttf", FontSize = 8, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "11.075 -126.612", OffsetMax = "126.125 -112.988" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "ModerationIcon",
                    Parent = UI_Chat_Context,
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage("IQCHAT_MODERATION_ICON") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "141.88 -125.3", OffsetMax = "152.88 -114.3" }
                }
                });


                container.Add(new CuiElement
                {
                    Name = "ModeratorMuteMenu",
                    Parent = UI_Chat_Context,
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage("IQCHAT_ELEMENT_PANEL_ICON")},
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "11.071 -144.188", OffsetMax = "152.881 -129.752" }
                }
                });

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0.95" },
                    Button = { Command = "%COMMAND_MUTE_MENU%", Color = "0 0 0 0" },
                    Text = { Text = "%TEXT_MUTE_MENU%", FontSize = 9, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf" }
                }, "ModeratorMuteMenu", "ModeratorMuteMenu_Btn");


                AddInterface("UI_Chat_Moderation", container.ToJson());
            }
            private void BuildingMuteAllChat()
            {
                CuiElementContainer container = new CuiElementContainer();

                container.Add(new CuiElement
                {
                    Name = "ModeratorMuteAllChat",
                    Parent = UI_Chat_Context,
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage("IQCHAT_ELEMENT_PANEL_ICON") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "11.07 -161.818", OffsetMax = "152.88 -147.382" }
                }
                });

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0.95" },
                    Button = { Command = "%COMMAND_MUTE_ALLCHAT%", Color = "0 0 0 0" },
                    Text = { Text = "%TEXT_MUTE_ALLCHAT%", FontSize = 9, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf" }
                }, "ModeratorMuteAllChat", "ModeratorMuteAllChat_Btn");

                AddInterface("UI_Chat_Administation_AllChat", container.ToJson());
            }
            private void BuildingMuteAllVoice()
            {
                CuiElementContainer container = new CuiElementContainer();

                container.Add(new CuiElement
                {
                    Name = "ModeratorMuteAllVoice",
                    Parent = UI_Chat_Context,
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage("IQCHAT_ELEMENT_PANEL_ICON") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "11.075 -179.448", OffsetMax = "152.885 -165.012" }
                }
                });

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0.95" },
                    Button = { Command = "%COMMAND_MUTE_ALLVOICE%", Color = "0 0 0 0" },
                    Text = { Text = "%TEXT_MUTE_ALLVOICE%", FontSize = 9, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf" }
                }, "ModeratorMuteAllVoice", "ModeratorMuteAllVoice_Btn");

                AddInterface("UI_Chat_Administation_AllVoce", container.ToJson());
            }

            
            
                        private void BuildingAlertUI()
            {
                CuiElementContainer container = new CuiElementContainer();

                container.Add(new CuiElement
                {
                    Name = UI_Chat_Alert,
                    Parent = "Overlay",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage("IQCHAT_ALERT_PANEL") },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "0 -136.5", OffsetMax = "434 -51.5" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "AlertTitle",
                    Parent = UI_Chat_Alert,
                    Components = {
                    new CuiTextComponent { Text = "<b>%TITLE%</b>", Font = "robotocondensed-bold.ttf", FontSize = 16, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-184.193 9.119", OffsetMax = "189.223 30.925" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "AlertText",
                    Parent = UI_Chat_Alert,
                    Components = {
                    new CuiTextComponent { Text = "%DESCRIPTION%", Font = "robotocondensed-regular.ttf", FontSize = 12, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-184.193 -27.133", OffsetMax = "189.223 9.119" }
                }
                });

                AddInterface("UI_Chat_Alert", container.ToJson());
            }
                    }

        public string FindFakeName(ulong userID) => (string)IQFakeActive?.Call("FindFakeName", userID);
        String IQRankGetTimeGame(ulong userID) => (string)(IQRankSystem?.Call("API_GET_TIME_GAME", userID));

        [ConsoleCommand("online")]
        private void ShowPlayerOnlineConsole(ConsoleSystem.Arg arg)
        {
            if (!config.OtherSetting.UseCommandOnline) return;

            BasePlayer player = arg.Player();
            List<String> PlayerNames = GetPlayersOnline();
            String Message = GetLang("IQCHAT_INFO_ONLINE", player != null ? player.UserIDString : null, String.Join($"\n", PlayerNames));

            if (player != null)
                player.ConsoleMessage(Message);
            else
            {
                String Pattern = @"</?size.*?>|</?color.*?>";
                String Messages = Regex.IsMatch(Message, Pattern) ? Regex.Replace(Message, Pattern, "") : Message;
                Puts(Messages);
            }
        }
        
        
                private void DrawUI_IQChat_Ignore_Alert(BasePlayer player, BasePlayer Target, UInt64 IDFake = 0)
        {
            String InterfacePanel = InterfaceBuilder.GetInterface("UI_Chat_Mute_And_Ignore_Alert_Panel");
            String Interface = InterfaceBuilder.GetInterface("UI_Chat_Ignore_Alert");
            if (Interface == null || InterfacePanel == null) return;

            GeneralInformation.RenameInfo Renamer = (IQFakeActive && Target == null && IDFake != 0) ? null : GeneralInfo.GetInfoRename(Target.userID);
            String NickNamed = (IQFakeActive && Target == null && IDFake != 0) ? FindFakeName(IDFake) : Renamer != null ? $"{Renamer.RenameNick ?? Target.displayName}" : Target.displayName;
		   		 		  						  	   		  	  			  	  			  	 				  	  	
            Interface = Interface.Replace("%TITLE%", GetLang(UserInformation[player.userID].Settings.IsIgnored((IQFakeActive && Target == null && IDFake != 0) ? IDFake : Target.userID) ? "IQCHAT_TITLE_IGNORE_TITLES_UNLOCK" : "IQCHAT_TITLE_IGNORE_TITLES", player.UserIDString, NickNamed));
            Interface = Interface.Replace("%BUTTON_YES%", GetLang("IQCHAT_TITLE_IGNORE_BUTTON_YES", player.UserIDString));
            Interface = Interface.Replace("%BUTTON_NO%", GetLang("IQCHAT_TITLE_IGNORE_BUTTON_NO", player.UserIDString));
            Interface = Interface.Replace("%COMMAND%", $"newui.cmd action.mute.ignore ignore.and.mute.controller {SelectedAction.Ignore} confirm.yes {((IQFakeActive && Target == null && IDFake != 0) ? IDFake : Target.userID)}");

            CuiHelper.DestroyUi(player, "MUTE_AND_IGNORE_PANEL_ALERT");
            CuiHelper.AddUi(player, InterfacePanel);
            CuiHelper.AddUi(player, Interface);
        }
        private void DrawUI_IQChat_OpenDropList(BasePlayer player, TakeElementUser ElementType, Int32 Page = 0)
        {
            String Interface = InterfaceBuilder.GetInterface("UI_Chat_OpenDropList");
            if (Interface == null) return;

            if (!LocalBase.ContainsKey(player)) return;

            String Title = String.Empty;
            String Description = String.Empty;
            List<Configuration.ControllerParameters.AdvancedFuncion> InfoUI = new List<Configuration.ControllerParameters.AdvancedFuncion>();

            switch (ElementType)
            {
                case TakeElementUser.MultiPrefix:
                case TakeElementUser.Prefix:
                    {
                        InfoUI = LocalBase[player].ElementsPrefix;
                        Title = GetLang("IQCHAT_CONTEXT_SLIDER_PREFIX_TITLE", player.UserIDString);
                        Description = GetLang("IQCHAT_CONTEXT_DESCRIPTION_PREFIX", player.UserIDString);
                        break;
                    }
                case TakeElementUser.Nick:
                    {
                        InfoUI = LocalBase[player].ElementsNick;
                        Title = GetLang("IQCHAT_CONTEXT_SLIDER_NICK_COLOR_TITLE", player.UserIDString);
                        Description = GetLang("IQCHAT_CONTEXT_DESCRIPTION_NICK", player.UserIDString);
                        break;
                    }
                case TakeElementUser.Chat:
                    {
                        InfoUI = LocalBase[player].ElementsChat;
                        Title = GetLang("IQCHAT_CONTEXT_SLIDER_MESSAGE_COLOR_TITLE", player.UserIDString);
                        Description = GetLang("IQCHAT_CONTEXT_DESCRIPTION_CHAT", player.UserIDString);
                        break;
                    }
                case TakeElementUser.Rank:
                    {
                        InfoUI = LocalBase[player].ElementsRanks;
                        Title = GetLang("IQCHAT_CONTEXT_SLIDER_IQRANK_TITLE", player.UserIDString);
                        Description = GetLang("IQCHAT_CONTEXT_DESCRIPTION_RANK", player.UserIDString);
                        break;
                    }
                default:
                    break;
            }

            //  if (InfoUI == null || InfoUI.Count == 0) return;

            Interface = Interface.Replace("%TITLE%", Title);
            Interface = Interface.Replace("%DESCRIPTION%", Description);

            String CommandRight = InfoUI.Skip(9 * (Page + 1)).Count() > 0 ? $"newui.cmd droplist.controller page.controller {ElementType} + {Page}" : String.Empty;
            String CommandLeft = Page != 0 ? $"newui.cmd droplist.controller page.controller {ElementType} - {Page}" : String.Empty;

            Interface = Interface.Replace("%NEXT_BTN%", CommandRight);
            Interface = Interface.Replace("%BACK_BTN%", CommandLeft);

            Interface = Interface.Replace("%COLOR_RIGHT%", String.IsNullOrWhiteSpace(CommandRight) ? "1 1 1 0.1" : "1 1 1 1");
            Interface = Interface.Replace("%COLOR_LEFT%", String.IsNullOrWhiteSpace(CommandLeft) ? "1 1 1 0.1" : "1 1 1 1");

            CuiHelper.DestroyUi(player, "OpenDropList");
            CuiHelper.AddUi(player, Interface);
		   		 		  						  	   		  	  			  	  			  	 				  	  	
            Int32 Count = 0;
            Int32 X = 0, Y = 0;
            foreach (Configuration.ControllerParameters.AdvancedFuncion Info in InfoUI.Skip(9 * Page).Take(9))
            {
                DrawUI_IQChat_OpenDropListArgument(player, ElementType, Info, X, Y, Count);

                if (ElementType == TakeElementUser.MultiPrefix && UserInformation[player.userID].Info.PrefixList.Contains(Info.Argument))
                    DrawUI_IQChat_OpenDropListArgument(player, Count);

                Count++;
                X++;
                if (X == 3)
                {
                    X = 0;
                    Y++;
                }
            }
        }
        
        
        
        
        public List<String> GetMesagesList(BasePlayer player, Dictionary<String, List<String>> LanguageMessages)
        {
            String LangPlayer = _.lang.GetLanguage(player.UserIDString);

            if (LanguageMessages.ContainsKey(LangPlayer))
                return LanguageMessages[LangPlayer];
            else if (LanguageMessages.ContainsKey("en"))
                return LanguageMessages["en"];
            else return LanguageMessages.FirstOrDefault().Value;
        }
        private const String PermissionAlert = "iqchat.alertuse";

        
        private BasePlayer GetPlayerNickOrID(String Info)
        {
            String NameOrID = String.Empty;

            KeyValuePair<UInt64, GeneralInformation.RenameInfo> RenameInformation = GeneralInfo.RenameList.FirstOrDefault(x => x.Value.RenameNick.Contains(Info) || x.Value.RenameID.ToString() == Info);
            if (RenameInformation.Value == null)
                NameOrID = Info;
            else NameOrID = RenameInformation.Key.ToString();

            foreach (BasePlayer Finder in BasePlayer.activePlayerList)
            {
                if (Finder.displayName.ToLower().Contains(NameOrID.ToLower()) || Finder.userID.ToString() == NameOrID)
                    return Finder;
            }
		   		 		  						  	   		  	  			  	  			  	 				  	  	
            return null;
        }
        private void DrawUI_IQChat_OpenDropListArgument(BasePlayer player, Int32 Count)
        {
            String Interface = InterfaceBuilder.GetInterface("UI_Chat_OpenDropListArgument_Taked");
            if (Interface == null) return;

            Interface = Interface.Replace("%COUNT%", Count.ToString());

            CuiHelper.DestroyUi(player, $"TAKED_INFO_{Count}");
            CuiHelper.AddUi(player, Interface);
        }
        
        
        [ChatCommand("chat")]
        private void ChatCommandOpenedUI(BasePlayer player)
        {
            if (_interface == null)
            {
                PrintWarning(LanguageEn ? "We generate the interface, wait for a message about successful generation" : "Генерируем интерфейс, ожидайте сообщения об успешной генерации");
                return;
            }
            if (player == null) return;
		   		 		  						  	   		  	  			  	  			  	 				  	  	
            Configuration.ControllerParameters ControllerParameters = config.ControllerParameter;

            if (!LocalBase.ContainsKey(player))
                LocalBase.Add(player, new InformationOpenedUI { });

            LocalBase[player].ElementsPrefix = ControllerParameters.Prefixes.Prefixes.OrderByDescending(arg => arg.Argument.Length).Where(p => permission.UserHasPermission(player.UserIDString, p.Permissions) && !p.IsBlockSelected).ToList();
            LocalBase[player].ElementsNick = ControllerParameters.NickColorList.Where(n => permission.UserHasPermission(player.UserIDString, n.Permissions) && !n.IsBlockSelected).ToList();
            LocalBase[player].ElementsChat = ControllerParameters.MessageColorList.Where(m => permission.UserHasPermission(player.UserIDString, m.Permissions) && !m.IsBlockSelected).ToList();

            if (IQRankSystem && config.ReferenceSetting.IQRankSystems.UseRankSystem)
            {
                List<Configuration.ControllerParameters.AdvancedFuncion> RankList = new List<Configuration.ControllerParameters.AdvancedFuncion>();
                foreach (String Rank in IQRankListKey(player.userID))
                    RankList.Add(new Configuration.ControllerParameters.AdvancedFuncion { Argument = Rank, Permissions = String.Empty });

                LocalBase[player].ElementsRanks = RankList;
            }

            DrawUI_IQChat_Context(player);
        }
        String API_GET_CHAT_COLOR(UInt64 ID)
        {
            if (!UserInformation.ContainsKey(ID)) return String.Empty;

            return UserInformation[ID].Info.ColorMessage;
        }
        private class InformationOpenedUI
        {
            public List<Configuration.ControllerParameters.AdvancedFuncion> ElementsPrefix;
            public List<Configuration.ControllerParameters.AdvancedFuncion> ElementsNick;
            public List<Configuration.ControllerParameters.AdvancedFuncion> ElementsChat;
            public List<Configuration.ControllerParameters.AdvancedFuncion> ElementsRanks;
            public Int32 SlideIndexPrefix = 0;
            public Int32 SlideIndexNick = 0;
            public Int32 SlideIndexChat = 0;
            public Int32 SlideIndexRank = 0;
        }
        public String FormatTime(Double Second, String UserID = null)
        {
            TimeSpan time = TimeSpan.FromSeconds(Second);
            String Result = String.Empty;
            String Days = GetLang("TITLE_FORMAT_DAYS", UserID);
            String Hourse = GetLang("TITLE_FORMAT_HOURSE", UserID);
            String Minutes = GetLang("TITLE_FORMAT_MINUTES", UserID);
            String Seconds = GetLang("TITLE_FORMAT_SECONDS", UserID);

            if (time.Seconds != 0)
                Result = $"{Format(time.Seconds, Seconds, Seconds, Seconds)}";

            if (time.Minutes != 0)
                Result = $"{Format(time.Minutes, Minutes, Minutes, Minutes)}";

            if (time.Hours != 0)
                Result = $"{Format(time.Hours, Hourse, Hourse, Hourse)}";

            if (time.Days != 0)
                Result = $"{Format(time.Days, Days, Days, Days)}";

            return Result;
        }
        
        
        [ChatCommand("pm")]
        void PmChat(BasePlayer Sender, String cmd, String[] arg)
        {
            Configuration.ControllerMessage ControllerMessages = config.ControllerMessages;
            if (!ControllerMessages.TurnedFunc.PMSetting.PMActivate) return;
            if (arg.Length == 0 || arg == null)
            {
                ReplySystem(Sender, lang.GetMessage("COMMAND_PM_NOTARG", this, Sender.UserIDString));
                return;
            }

            Configuration.ControllerMessage.TurnedFuncional.AntiNoob.Settings antiNoob = config.ControllerMessages.TurnedFunc.AntiNoobSetting.AntiNoobPM;
            if (antiNoob.AntiNoobActivate)
                if (IsNoob(Sender.userID, antiNoob.TimeBlocked))
                {
                    ReplySystem(Sender, GetLang("IQCHAT_INFO_ANTI_NOOB_PM", Sender.UserIDString, FormatTime(UserInformationConnection[Sender.userID].LeftTime(antiNoob.TimeBlocked), Sender.UserIDString)));
                    return;
                }

            String NameUser = arg[0];

            if (config.ReferenceSetting.IQFakeActiveSettings.UseIQFakeActive)
                if (IQFakeActive)
                    if (IsFake(NameUser))
                    {
                        ReplySystem(Sender, GetLang("COMMAND_PM_SUCCESS", Sender.UserIDString, string.Join(" ", arg.ToArray()).Replace(NameUser, ""), NameUser));
                        return;
                    }

            BasePlayer TargetUser = GetPlayerNickOrID(NameUser);
            if (TargetUser == null || NameUser == null || !UserInformation.ContainsKey(TargetUser.userID))
            {
                ReplySystem(Sender, GetLang("COMMAND_PM_NOT_USER", Sender.UserIDString));
                return;
            }
		   		 		  						  	   		  	  			  	  			  	 				  	  	
            User InfoTarget = UserInformation[TargetUser.userID];
            User InfoSender = UserInformation[Sender.userID];
            if (!InfoTarget.Settings.TurnPM)
            {
                ReplySystem(Sender, GetLang("FUNC_MESSAGE_PM_TURN_FALSE", Sender.UserIDString));
                return;
            }

            if (ControllerMessages.TurnedFunc.IgnoreUsePM)
            {
                if (InfoTarget.Settings.IsIgnored(Sender.userID))
                {
                    ReplySystem(Sender, GetLang("IGNORE_NO_PM", Sender.UserIDString));
                    return;
                }
                if (InfoSender.Settings.IsIgnored(TargetUser.userID))
                {
                    ReplySystem(Sender, GetLang("IGNORE_NO_PM_ME", Sender.UserIDString));
                    return;
                }
            }
            String Message = GetMessageInArgs(Sender, arg.Skip(1).ToArray());

            if (Message == null || Message.Length <= 0)
            {
                ReplySystem(Sender, GetLang("COMMAND_PM_NOT_NULL_MSG", Sender.UserIDString));
                return;
            }
            Message = Message.EscapeRichText();

            if (Message.Length > 125) return;

            PMHistory[TargetUser] = Sender;
            PMHistory[Sender] = TargetUser;

            GeneralInformation.RenameInfo RenamerSender = GeneralInfo.GetInfoRename(Sender.userID);
            GeneralInformation.RenameInfo RenamerTarget = GeneralInfo.GetInfoRename(TargetUser.userID);

            String DisplayNameSender = RenamerSender != null ? RenamerSender.RenameNick ?? Sender.displayName : Sender.displayName;
            String TargetDisplayName = RenamerTarget != null ? RenamerTarget.RenameNick ?? TargetUser.displayName : TargetUser.displayName;
            ReplySystem(TargetUser, GetLang("COMMAND_PM_SEND_MSG", TargetUser.UserIDString, DisplayNameSender, Message));
            ReplySystem(Sender, GetLang("COMMAND_PM_SUCCESS", Sender.UserIDString, Message, TargetDisplayName));

            if (InfoTarget.Settings.TurnSound)
                Effect.server.Run(ControllerMessages.TurnedFunc.PMSetting.SoundPM, TargetUser.GetNetworkPosition());

            Log(LanguageEn ? $"PRIVATE MESSAGES : {Sender.userID}({Sender.displayName}) sent a message to the player - {TargetUser.displayName}({TargetDisplayName})\nMESSAGE : {Message}" : $"ЛИЧНЫЕ СООБЩЕНИЯ : {Sender.userID}({Sender.displayName}) отправил сообщение игроку - {TargetUser.displayName}({TargetDisplayName})\nСООБЩЕНИЕ : {Message}");
            DiscordLoggPM(Sender, TargetUser, Message);
		   		 		  						  	   		  	  			  	  			  	 				  	  	
            RCon.Broadcast(RCon.LogType.Chat, new Chat.ChatEntry
            {
                Message = LanguageEn ? $"PRIVATE MESSAGES : {Sender.displayName}({Sender.userID}) -> {TargetUser.displayName} : MESSAGE : {Message}" : $"ЛИЧНЫЕ СООБЩЕНИЯ : {Sender.displayName}({Sender.userID}) -> {TargetUser.displayName} : СООБЩЕНИЕ : {Message}",
                UserId = Sender.UserIDString,
                Username = Sender.displayName,
                Channel = Chat.ChatChannel.Global,
                Time = (DateTime.UtcNow.Hour * 3600) + (DateTime.UtcNow.Minute * 60),
                Color = "#3f4bb8",
            });
            PrintWarning(LanguageEn ? $"PRIVATE MESSAGES : {Sender.displayName}({Sender.userID}) -> {TargetUser.displayName} : MESSAGE : {Message}" : $"ЛИЧНЫЕ СООБЩЕНИЯ : {Sender.displayName}({Sender.userID}) -> {TargetUser.displayName} : СООБЩЕНИЕ : {Message}");
        }
		   		 		  						  	   		  	  			  	  			  	 				  	  	
        
        
                private const Boolean LanguageEn = false;

        
                void ReplyChat(Chat.ChatChannel channel, BasePlayer player, String OutMessage, String FormatPlayer)
        {
            Configuration.ControllerMessage ControllerMessages = config.ControllerMessages;

            User Info = UserInformation[player.userID];
            GeneralInformation.RenameInfo RenameInfo = GeneralInfo.GetInfoRename(player.userID);
            UInt64 RenameID = RenameInfo != null ? RenameInfo.RenameID != 0 ? RenameInfo.RenameID : player.userID : player.userID;

            if (channel == Chat.ChatChannel.Global)
            {
                foreach (BasePlayer p in BasePlayer.activePlayerList)
                {
                    if (OutMessage.Contains("@"))
                    {
                        String SplittedName = OutMessage.Substring(OutMessage.IndexOf('@')).Replace("@", "").Split(' ')[0];

                        BasePlayer playerTags = GetPlayerNickOrID(SplittedName);

                        if (playerTags != null)
                        {
                            User InfoP = UserInformation[playerTags.userID];

                            if (InfoP.Settings.TurnAlert && p == playerTags)
                            {
                                ReplySystem(p, $"<size=16>{OutMessage.Trim()}</size>", GetLang("IQCHAT_FUNCED_ALERT_TITLE", p.UserIDString), p.UserIDString, ControllerMessages.GeneralSetting.AlertFormat.AlertPlayerColor);
                                if (InfoP.Settings.TurnSound)
                                    Effect.server.Run(ControllerMessages.GeneralSetting.AlertFormat.SoundAlertPlayer, playerTags.GetNetworkPosition());
                            }
                            else p.SendConsoleCommand("chat.add", (int)channel, RenameID, $"{FormatPlayer}: {OutMessage}");
                            //else p.SendConsoleCommand("chat.add", new object[] { (int)channel, RenameID, OutMessage });
                        }
                        else p.SendConsoleCommand("chat.add", (int)channel, RenameID, $"{FormatPlayer}: {OutMessage}");
                    }
                    else p.SendConsoleCommand("chat.add", (int)channel, RenameID, $"{FormatPlayer}: {OutMessage}");

                    p.ConsoleMessage($"{FormatPlayer} {OutMessage}");
                }
            }
            if (channel == Chat.ChatChannel.Team)
            {
                RelationshipManager.PlayerTeam Team = RelationshipManager.ServerInstance.FindTeam(player.currentTeam);
                if (Team == null) return;
                foreach (var FindPlayers in Team.members)
                {
                    BasePlayer TeamPlayer = BasePlayer.FindByID(FindPlayers);
                    if (TeamPlayer == null) continue;

                    TeamPlayer.SendConsoleCommand("chat.add", (int)channel, RenameID, $"{FormatPlayer}: {OutMessage}");
                }
            }
            if (channel == Chat.ChatChannel.Cards)
            {
                if (!player.isMounted)
                    return;

                CardTable cardTable = player.GetMountedVehicle() as CardTable;
                if (cardTable == null || !cardTable.GameController.PlayerIsInGame(player))
                    return;

                List<Network.Connection> PlayersCards = new List<Network.Connection>();
                cardTable.GameController.GetConnectionsInGame(PlayersCards);
                if (PlayersCards == null || PlayersCards.Count == 0)
                    return;

                foreach (Network.Connection PCard in PlayersCards)
                {
                    BasePlayer PlayerInRound = BasePlayer.FindByID(PCard.userid);
                    if (PlayerInRound == null) return;
                    PlayerInRound.SendConsoleCommand("chat.add", (int)channel, RenameID, $"{FormatPlayer}: {OutMessage}");
                }
            }
        }
        public List<String> KeyImages = new List<String>
        {
            "UI_IQCHAT_CONTEXT_NO_RANK",
            "UI_IQCHAT_CONTEXT_RANK",
            "IQCHAT_INFORMATION_ICON",
            "IQCHAT_SETTING_ICON",
            "IQCHAT_IGNORE_INFO_ICON",
            "IQCHAT_MODERATION_ICON",
            "IQCHAT_ELEMENT_PANEL_ICON",
            "IQCHAT_ELEMENT_PREFIX_MULTI_TAKE_ICON",
            "IQCHAT_ELEMENT_SLIDER_ICON",
            "IQCHAT_ELEMENT_SLIDER_LEFT_ICON",
            "IQCHAT_ELEMENT_SLIDER_RIGHT_ICON",
            "IQCHAT_ELEMENT_DROP_LIST_OPEN_ICON",
            "IQCHAT_ELEMENT_DROP_LIST_OPEN_ARGUMENT_ICON",
            "IQCHAT_ELEMENT_DROP_LIST_OPEN_TAKED",
            "IQCHAT_ELEMENT_SETTING_CHECK_BOX",
            "IQCHAT_ALERT_PANEL",
            "IQCHAT_MUTE_AND_IGNORE_PANEL",
            "IQCHAT_MUTE_AND_IGNORE_ICON",
            "IQCHAT_MUTE_AND_IGNORE_SEARCH",
            "IQCHAT_MUTE_AND_IGNORE_PAGE_PANEL",
            "IQCHAT_MUTE_AND_IGNORE_PLAYER",
            "IQCHAT_MUTE_AND_IGNORE_PLAYER_STATUS",
            "IQCHAT_IGNORE_ALERT_PANEL",
            "IQCHAT_IGNORE_ALERT_ICON",
            "IQCHAT_IGNORE_ALERT_BUTTON_YES",
            "IQCHAT_IGNORE_ALERT_BUTTON_NO",
            "IQCHAT_MUTE_ALERT_PANEL",
            "IQCHAT_MUTE_ALERT_ICON",
            "IQCHAT_MUTE_ALERT_PANEL_REASON",
        };
        public Dictionary<BasePlayer, BasePlayer> PMHistory = new Dictionary<BasePlayer, BasePlayer>();
        
        private void DrawUI_IQChat_Mute_And_Ignore(BasePlayer player, SelectedAction Action)
        {
            String Interface = InterfaceBuilder.GetInterface("UI_Chat_Mute_And_Ignore");
            if (Interface == null) return;

            Interface = Interface.Replace("%TITLE%", Action == SelectedAction.Mute ? GetLang("IQCHAT_TITLE_IGNORE_AND_MUTE_MUTED", player.UserIDString) : GetLang("IQCHAT_TITLE_IGNORE_AND_MUTE_IGNORED", player.UserIDString));
            Interface = Interface.Replace("%ACTION_TYPE%", $"{Action}");

            CuiHelper.DestroyUi(player, "MuteAndIgnoredPanel");
            CuiHelper.AddUi(player, Interface);

            DrawUI_IQChat_Mute_And_Ignore_Player_Panel(player, Action);
        }
        void OnPlayerCommand(BasePlayer player, string command, string[] args)
        {
            DiscordLoggCommand(player, command, args);
        }

                
        private String GetClanTag(UInt64 playerID)
        {
            if (!Clans) return String.Empty;
            if (!config.ReferenceSetting.ClansSettings.UseClanTag) return String.Empty;
            String ClanTag = (String)Clans?.CallHook("GetClanOf", playerID);

            return String.IsNullOrWhiteSpace(ClanTag) ? String.Empty : GetLang("CLANS_SYNTAX_PREFIX", playerID.ToString(), ClanTag);
        }

        [ConsoleCommand("hunmute")]
        void HideUnMuteConsole(ConsoleSystem.Arg arg)
        {
            if (arg.Player() != null)
                if (!permission.UserHasPermission(arg.Player().UserIDString, PermissionMute)) return;
            if (arg == null || arg.Args == null || arg.Args.Length != 1 || arg.Args.Length > 1)
            {
                PrintWarning(LanguageEn ? "Invalid syntax, please use : hunmute Steam64ID" : "Неверный синтаксис,используйте : hunmute Steam64ID");
                return;
            }
            string NameOrID = arg.Args[0];
            BasePlayer target = GetPlayerNickOrID(NameOrID);
            if (target == null)
            {
                UInt64 Steam64ID = 0;
                if (UInt64.TryParse(NameOrID, out Steam64ID))
                {
                    if (UserInformation.ContainsKey(Steam64ID))
                    {
                        User Info = UserInformation[Steam64ID];
                        if (Info == null) return;
                        if (!Info.MuteInfo.IsMute(MuteType.Chat))
                        {
                            ConsoleOrPrintMessage(arg.Player(),
                                LanguageEn ? "The player does not have a chat lock" : "У игрока нет блокировки чата");
                            return;
                        }
		   		 		  						  	   		  	  			  	  			  	 				  	  	
                        Info.MuteInfo.UnMute(MuteType.Chat);
		   		 		  						  	   		  	  			  	  			  	 				  	  	
                        ConsoleOrPrintMessage(arg.Player(),
                            LanguageEn ? "You have unblocked the offline chat to the player" : "Вы разблокировали чат offline игроку");
                        return;
                    }
                    else
                    {
                        ConsoleOrPrintMessage(arg.Player(),
                            LanguageEn ? "This player is not on the server" : "Такого игрока нет на сервере");
                        return;
                    }
                }
                else
                {
                    ConsoleOrPrintMessage(arg.Player(),
                        LanguageEn ? "This player is not on the server" : "Такого игрока нет на сервере");
                    return;
                }
            }

            UnmutePlayer(target, MuteType.Chat, arg.Player(), true, true);
        }
        [ConsoleCommand("adminalert")]
        private void AdminAlertConsoleCommand(ConsoleSystem.Arg args)
        {
            BasePlayer Sender = args.Player();
            if (Sender != null)
                if (!permission.UserHasPermission(Sender.UserIDString, PermissionAlert)) return;
            Alert(Sender, args.Args, true);
        }
        private const String PermissionMute = "iqchat.muteuse";
        private void DrawUI_IQChat_Update_MuteVoice_All(BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString, PermissionMutedAdmin)) return;

            String InterfaceAdministratorVoice = InterfaceBuilder.GetInterface("UI_Chat_Administation_AllVoce");
            if (InterfaceAdministratorVoice == null) return;

            InterfaceAdministratorVoice = InterfaceAdministratorVoice.Replace("%TEXT_MUTE_ALLVOICE%", GetLang(!GeneralInfo.TurnMuteAllVoice ? "IQCHAT_BUTTON_MODERATION_MUTE_ALL_VOICE" : "IQCHAT_BUTTON_MODERATION_UNMUTE_ALL_VOICE", player.UserIDString));
            InterfaceAdministratorVoice = InterfaceAdministratorVoice.Replace("%COMMAND_MUTE_ALLVOICE%", $"newui.cmd action.mute.ignore mute.controller {SelectedAction.Mute} mute.all.voice");

            CuiHelper.DestroyUi(player, "ModeratorMuteAllVoice");
            CuiHelper.AddUi(player, InterfaceAdministratorVoice);
        }
        private string StripHtmlTags(string input)
        {
            return Regex.Replace(input, "<.*?>", String.Empty);
        }
		   		 		  						  	   		  	  			  	  			  	 				  	  	
        
                void OnGroupPermissionGranted(string name, string perm)
        {
            String[] PlayerGroups = permission.GetUsersInGroup(name);
            if (PlayerGroups == null) return;

            foreach (String playerInfo in PlayerGroups)
            {
                BasePlayer player = BasePlayer.FindByID(UInt64.Parse(playerInfo.Substring(0, 17)));
                if (player == null) return;

                SetupParametres(player.UserIDString, perm);
            }
        }

        [ChatCommand("ignore")]
        void IgnorePlayerPM(BasePlayer player, String cmd, String[] arg)
        {
            Configuration.ControllerMessage ControllerMessages = config.ControllerMessages;
            if (!ControllerMessages.TurnedFunc.IgnoreUsePM) return;

            User Info = UserInformation[player.userID];

            if (arg.Length == 0 || arg == null)
            {
                ReplySystem(player, GetLang("INGORE_NOTARG", player.UserIDString));
                return;
            }
            String NameUser = arg[0];
            BasePlayer TargetUser = BasePlayer.Find(NameUser);
		   		 		  						  	   		  	  			  	  			  	 				  	  	
            if (TargetUser == null || NameUser == null)
            {
                ReplySystem(player, GetLang("COMMAND_PM_NOT_USER", player.UserIDString));
                return;
            }

            String Lang = !Info.Settings.IsIgnored(TargetUser.userID) ? GetLang("IGNORE_ON_PLAYER", player.UserIDString, TargetUser.displayName) : GetLang("IGNORE_OFF_PLAYER", player.UserIDString, TargetUser.displayName);
            ReplySystem(player, Lang);

            Info.Settings.IgnoredAddOrRemove(TargetUser.userID);
        }

        private void DrawUI_IQChat_Mute_And_Ignore_Player_Panel(BasePlayer player, SelectedAction Action, Int32 Page = 0, String SearchName = null)
        {
            String Interface = InterfaceBuilder.GetInterface("UI_Chat_Mute_And_Ignore_Panel_Content");
            if (Interface == null) return;

            CuiHelper.DestroyUi(player, "MuteIgnorePanelContent");
            CuiHelper.AddUi(player, Interface);

            if (IQFakeActive)
            {
                var FakePlayerList = Action == SelectedAction.Mute ? SearchName != null ? PlayerBases.Where(p => p.DisplayName.ToLower().Contains(SearchName.ToLower())).OrderByDescending(p => !IsFake(p.UserID) && UserInformation.ContainsKey(p.UserID) && (UserInformation[p.UserID].MuteInfo.IsMute(MuteType.Chat) || UserInformation[p.UserID].MuteInfo.IsMute(MuteType.Voice))) : PlayerBases.OrderByDescending(p => !IsFake(p.UserID) && UserInformation.ContainsKey(p.UserID) && (UserInformation[p.UserID].MuteInfo.IsMute(MuteType.Chat) || UserInformation[p.UserID].MuteInfo.IsMute(MuteType.Voice))) :
                                                                            SearchName != null ? PlayerBases.Where(p => p.DisplayName.ToLower().Contains(SearchName.ToLower())).OrderByDescending(p => !IsFake(p.UserID) && UserInformation.ContainsKey(p.UserID) && (UserInformation[player.userID].Settings.IgnoreUsers.Contains(p.UserID))) : PlayerBases.OrderByDescending(p => !IsFake(p.UserID) && UserInformation.ContainsKey(p.UserID) && (UserInformation[player.userID].Settings.IgnoreUsers.Contains(p.UserID)));

                DrawUI_IQChat_Mute_And_Ignore_Pages(player, (Boolean)(FakePlayerList.Skip(18 * (Page + 1)).Count() > 0), Action, Page);
                DrawUI_IQChat_Mute_And_Ignore_Player(player, Action, null, FakePlayerList.Skip(18 * Page).Take(18));
            }
            else
            {
                IOrderedEnumerable<BasePlayer> PlayerList = Action == SelectedAction.Mute ? SearchName != null ? BasePlayer.activePlayerList.Where(p => UserInformation.ContainsKey(p.userID) && p.displayName.ToLower().Contains(SearchName.ToLower())).OrderBy(p => UserInformation[p.userID].MuteInfo.IsMute(MuteType.Chat) || UserInformation[p.userID].MuteInfo.IsMute(MuteType.Voice)) : BasePlayer.activePlayerList.Where(p => UserInformation.ContainsKey(p.userID)).OrderBy(p => UserInformation[p.userID].MuteInfo.IsMute(MuteType.Chat) || UserInformation[p.userID].MuteInfo.IsMute(MuteType.Voice)) :
                                                                         SearchName != null ? BasePlayer.activePlayerList.Where(p => UserInformation.ContainsKey(p.userID) && p.displayName.ToLower().Contains(SearchName.ToLower())).OrderBy(p => UserInformation[player.userID].Settings.IgnoreUsers.Contains(p.userID)) : BasePlayer.activePlayerList.Where(p => UserInformation.ContainsKey(p.userID)).OrderBy(p => UserInformation[player.userID].Settings.IgnoreUsers.Contains(p.userID));

                DrawUI_IQChat_Mute_And_Ignore_Pages(player, (Boolean)(PlayerList.Skip(18 * (Page + 1)).Count() > 0), Action, Page);
                DrawUI_IQChat_Mute_And_Ignore_Player(player, Action, PlayerList.Skip(18 * Page).Take(18));
            }
        }
        void API_ALERT_PLAYER(BasePlayer player, String Message, String CustomPrefix = null, String CustomAvatar = null, String CustomHex = null) => ReplySystem(player, Message, CustomPrefix, CustomAvatar, CustomHex);
        public String GetMessages(BasePlayer player, Dictionary<String, List<String>> LanguageMessages)
        {
            String LangPlayer = _.lang.GetLanguage(player.UserIDString);

            if (LanguageMessages.ContainsKey(LangPlayer))
                return LanguageMessages[LangPlayer].GetRandom();
            else if (LanguageMessages.ContainsKey("en"))
                return LanguageMessages["en"].GetRandom();
            else return LanguageMessages.FirstOrDefault().Value.GetRandom();
        }
        private void AlertController(BasePlayer player)
        {
            Configuration.ControllerAlert.Alert Alert = config.ControllerAlertSetting.AlertSetting;
            Configuration.ControllerAlert.AdminSession AlertSessionAdmin = config.ControllerAlertSetting.AdminSessionSetting;
            Configuration.ControllerAlert.PlayerSession AlertSessionPlayer = config.ControllerAlertSetting.PlayerSessionSetting;
            Configuration.ControllerAlert.PersonalAlert AlertPersonal = config.ControllerAlertSetting.PersonalAlertSetting;
            GeneralInformation.RenameInfo RenameInformation = GeneralInfo.GetInfoRename(player.userID);
		   		 		  						  	   		  	  			  	  			  	 				  	  	
            String DisplayName = player.displayName;

            UInt64 UserID = player.userID;
            if (RenameInformation != null)
            {
                DisplayName = RenameInformation.RenameNick;
                UserID = RenameInformation.RenameID;
            }

            if (AlertSessionPlayer.ConnectedAlert)
            {
                if (!AlertSessionAdmin.ConnectedAlertAdmin)
                    if (player.IsAdmin) return;

                String Avatar = AlertSessionPlayer.ConnectedAvatarUse ? UserID.ToString() : String.Empty;

                if (AlertSessionPlayer.ConnectedWorld)
                {
                    String ipPlayer = player.IPlayer.Address;

                    if (player.net?.connection != null)
                    {
                        String[] ipPortPlayer = player.net.connection.ipaddress.Split(':');
                        if (ipPortPlayer.Length >= 1)
                            ipPlayer = ipPortPlayer[0]; 
                    }
                    
                    webrequest.Enqueue("http://ip-api.com/json/" + ipPlayer, null, (code, response) =>
                    {
                        if (code != 200 || response == null)
                            return;

                        String country = JsonConvert.DeserializeObject<Response>(response).Country;

                        if (!permission.UserHasPermission(player.UserIDString, PermissionHideConnection))
                        {
                            if (AlertSessionPlayer.ConnectionAlertRandom)
                                ReplyBroadcast(null, Avatar, false, AlertSessionPlayer.RandomConnectionAlert.LanguageMessages,DisplayName, country ?? "none");
                            else ReplyBroadcast(null, Avatar, false, "WELCOME_PLAYER_WORLD", DisplayName, country ?? "none");
                        }

                        Log($"[{player.userID}] {GetLang("WELCOME_PLAYER_WORLD", "", DisplayName, country ?? "none")}");
                    }, this);
                }
                else
                {
                    if (!permission.UserHasPermission(player.UserIDString, PermissionHideConnection))
                    {
                        if (AlertSessionPlayer.ConnectionAlertRandom)
                            ReplyBroadcast(null, Avatar, false,AlertSessionPlayer.RandomConnectionAlert.LanguageMessages, DisplayName);
                        else ReplyBroadcast(null, Avatar, false, "WELCOME_PLAYER", DisplayName);
                    }

                    Log($"[{player.userID}] {GetLang("WELCOME_PLAYER", "", DisplayName)}");
                }
            }
            if (AlertPersonal.UseWelcomeMessage)
            {
                String WelcomeMessage = GetMessages(player, AlertPersonal.WelcomeMessage.LanguageMessages);
                ReplySystem(player, WelcomeMessage);
            }
        }
        private enum ElementsSettingsType
        {
            PM,
            Broadcast,
            Alert,
            Sound
        }
        private void DrawUI_IQChat_Mute_Alert_Reasons(BasePlayer player, BasePlayer Target, MuteType Type, UInt64 IDFake = 0)
        {
            String Interface = InterfaceBuilder.GetInterface("UI_Chat_Mute_Alert_DropList_Title");
            if (Interface == null) return;

            Interface = Interface.Replace("%TITLE%", GetLang("IQCHAT_BUTTON_MODERATION_MUTE_MENU_TITLE_ALERT_REASON", player.UserIDString));

            CuiHelper.DestroyUi(player, "AlertMuteTitleReason");
            CuiHelper.DestroyUi(player, "PanelMuteReason");
            CuiHelper.AddUi(player, Interface);

            List<Configuration.ControllerMute.Muted> Reasons = Type == MuteType.Chat ? config.ControllerMutes.MuteChatReasons : config.ControllerMutes.MuteVoiceReasons;

            Int32 Y = 0;
            foreach (Configuration.ControllerMute.Muted Reason in Reasons.Take(6))
                DrawUI_IQChat_Mute_Alert_Reasons(player, Target, Reason.Reason, Y++, Type, IDFake);
        }

        private String GetReferenceTags(BasePlayer player)
        {
            String Result = String.Empty;
            String Rank = String.Empty;
            String RankTime = String.Empty;
            if (IQRankSystem)
            {
                Configuration.ReferenceSettings.IQRankSystem IQRank = config.ReferenceSetting.IQRankSystems;

                if (IQRank.UseRankSystem)
                {
                    if (IQRank.UseTimeStandart)
                        RankTime = String.IsNullOrWhiteSpace(IQRankGetTimeGame(player.userID)) ? String.Empty : String.Format(IQRank.FormatRank, IQRankGetTimeGame(player.userID));
                    Rank = String.IsNullOrWhiteSpace(IQRankGetRank(player.userID)) ? String.Empty : String.Format(IQRank.FormatRank, IQRankGetRank(player.userID));

                    if (!String.IsNullOrWhiteSpace(RankTime))
                        Result += $"{RankTime} ";
                    if (!String.IsNullOrWhiteSpace(Rank))
                        Result += $"{Rank} ";
                }
            }

            String XLevel = config.ReferenceSetting.XLevelsSettings.UseFullXLevels ? XLevel_GetPrefix(player) : XLevel_GetLevel(player);
            if (!String.IsNullOrWhiteSpace(XLevel))
                Result += $"{XLevel} ";

            String ClanTag = GetClanTag(player.userID);
            if (!String.IsNullOrWhiteSpace(ClanTag))
                Result += $"{ClanTag} ";

            return Result;
        }
		   		 		  						  	   		  	  			  	  			  	 				  	  	
        private void UnmutePlayer(BasePlayer Target, MuteType Type, BasePlayer Moderator = null, Boolean HideUnmute = false, Boolean Command = false)
        {
            if (!UserInformation.ContainsKey(Target.userID)) return;
            User Info = UserInformation[Target.userID];

            GeneralInformation.RenameInfo TargetRename = GeneralInfo.GetInfoRename(Target.userID);
            GeneralInformation.RenameInfo ModeratorRename = Moderator != null ? GeneralInfo.GetInfoRename(Moderator.userID) : null;
            if (!Info.MuteInfo.IsMute(Type))
            {
                if (Moderator != null)
                    ReplySystem(Moderator, LanguageEn ? "The player is not banned" : "У игрока нет блокировки");
                else Puts(LanguageEn ? "The player is not banned!" : "У игрока нет блокировки!");
                return;
            }

            String TargetName = TargetRename != null ? $"{TargetRename.RenameNick ?? Target.displayName}" : Target.displayName;
            String NameModerator = Moderator == null ? GetLang("IQCHAT_FUNCED_ALERT_TITLE_SERVER", Target.UserIDString) : ModeratorRename != null ? $"{ModeratorRename.RenameNick ?? Moderator.displayName}" : Moderator.displayName;
            String LangMessage = Type == MuteType.Chat ? "FUNC_MESSAGE_UNMUTE_CHAT" : "FUNC_MESSAGE_UNMUTE_VOICE";

            if (!HideUnmute)
                ReplyBroadcast(null, null, false, LangMessage, NameModerator, TargetName);
               // ReplyBroadcast(GetLang(LangMessage, Target.UserIDString, NameModerator, TargetName));
            else
            {
                if (Target != null)
                    ReplySystem(Target, GetLang(LangMessage, Target.UserIDString, NameModerator, TargetName));
                if (Moderator != null)
                    ReplySystem(Moderator, GetLang(LangMessage, Target.UserIDString, NameModerator, TargetName));
            }

            Info.MuteInfo.UnMute(Type);

            DiscordLoggMuted(Target, Type, Moderator: Moderator);
        }

        
        private void ConsoleOrPrintMessage(BasePlayer player, String Messages)
        {
            if (player != null)
                player.ConsoleMessage(Messages);
            else PrintWarning(Messages);
        }

        
                [ChatCommand("online")]
        private void ShowPlayerOnline(BasePlayer player)
        {
            if (!config.OtherSetting.UseCommandOnline) return;

            List<String> PlayerNames = GetPlayersOnline();
            String Message = GetLang("IQCHAT_INFO_ONLINE", player.UserIDString, String.Join($"\n", PlayerNames));
            ReplySystem(player, Message);
        }
        String API_GET_DEFAULT_MESSAGE_COLOR() => config.ControllerConnect.SetupDefaults.MessageDefault;

        private void DrawUI_IQChat_OpenDropListArgument(BasePlayer player, TakeElementUser ElementType, Configuration.ControllerParameters.AdvancedFuncion Info, Int32 X, Int32 Y, Int32 Count)
        {
            String Interface = InterfaceBuilder.GetInterface("UI_Chat_OpenDropListArgument");
            if (Interface == null) return;
            String Argument = ElementType == TakeElementUser.MultiPrefix || ElementType == TakeElementUser.Prefix ? Info.Argument :
                    ElementType == TakeElementUser.Nick ? $"<color={Info.Argument}>{player.displayName}</color>" :
                    ElementType == TakeElementUser.Chat ? $"<color={Info.Argument}>{GetLang("IQCHAT_CONTEXT_NICK_DISPLAY_MESSAGE", player.UserIDString)}</color>" :
                    ElementType == TakeElementUser.Rank ? IQRankGetNameRankKey(Info.Argument) : String.Empty;

            Interface = Interface.Replace("%OFFSET_MIN%", $"{-140.329 - (-103 * X)} {-2.243 + (Y * -28)}");
            Interface = Interface.Replace("%OFFSET_MAX%", $"{-65.271 - (-103 * X)} {22.568 + (Y * -28)}");
            Interface = Interface.Replace("%COUNT%", Count.ToString());
            Interface = Interface.Replace("%ARGUMENT%", Argument);
            Interface = Interface.Replace("%TAKE_COMMAND_ARGUMENT%", $"newui.cmd droplist.controller element.take {ElementType} {Count} {Info.Permissions} {Info.Argument}");

            CuiHelper.DestroyUi(player, $"ArgumentDropList_{Count}");
            CuiHelper.AddUi(player, Interface);
        }

        [ChatCommand("hmute")]
        void HideMute(BasePlayer Moderator, string cmd, string[] arg)
        {
            if (!permission.UserHasPermission(Moderator.UserIDString, PermissionMute)) return;
            if (arg == null || arg.Length != 3 || arg.Length > 3)
            {
                ReplySystem(Moderator, LanguageEn ? "Invalid syntax, use : hmute Steam64ID/Nick Reason Time(seconds)" : "Неверный синтаксис,используйте : hmute Steam64ID/Ник Причина Время(секунды)");
                return;
            }
            string NameOrID = arg[0];
            string Reason = arg[1];
            Int32 TimeMute = 0;
            if (!Int32.TryParse(arg[2], out TimeMute))
            {
                ReplySystem(Moderator, LanguageEn ? "Enter the time in numbers!" : "Введите время цифрами!");
                return;
            }
            BasePlayer target = GetPlayerNickOrID(NameOrID);
            if (target == null)
            {
                UInt64 Steam64ID = 0;
                if (UInt64.TryParse(NameOrID, out Steam64ID))
                {
                    if (UserInformation.ContainsKey(Steam64ID))
                    {
                        User Info = UserInformation[Steam64ID];
                        if (Info == null) return;
                        if (Info.MuteInfo.IsMute(MuteType.Chat))
                        {
                            ReplySystem(Moderator, LanguageEn ? "The player already has a chat lock" : "Игрок уже имеет блокировку чата");
                            return;
                        }

                        Info.MuteInfo.SetMute(MuteType.Chat, TimeMute);
                        ReplySystem(Moderator, LanguageEn ? "Chat blocking issued to offline player" : "Блокировка чата выдана offline-игроку");
                        return;
                    }
                    else
                    {
                        ReplySystem(Moderator, LanguageEn ? "This player is not on the server" : "Такого игрока нет на сервере");
                        return;
                    }
                }
                else
                {
                    ReplySystem(Moderator, LanguageEn ? "This player is not on the server" : "Такого игрока нет на сервере");
                    return;
                }
            }

            MutePlayer(target, MuteType.Chat, 0, Moderator, Reason, TimeMute, true, true);
        }

        private String RemoveLinkText(String text)
        {
            String hrefPattern = "([A-Za-z0-9-А-Яа-я]|https?://)[^ ]+\\.(com|lt|net|org|gg|ru|рф|int|info|ru.com|ru.net|com.ru|net.ru|рус|org.ru|moscow|biz|орг|su)";
            Regex rgx = new Regex(hrefPattern, RegexOptions.IgnoreCase);

            return config.ControllerMessages.Formatting.ControllerNickname.AllowedLinkNick.Contains(rgx.Match(text).Value) ? text : rgx.Replace(text, "").Trim();
        }

        [ChatCommand("r")]
        void RChat(BasePlayer Sender, string cmd, string[] arg)
        {
            Configuration.ControllerMessage ControllerMessages = config.ControllerMessages;
            if (!ControllerMessages.TurnedFunc.PMSetting.PMActivate) return;

            if (arg.Length == 0 || arg == null)
            {
                ReplySystem(Sender, GetLang("COMMAND_R_NOTARG", Sender.UserIDString));
                return;
            }

            Configuration.ControllerMessage.TurnedFuncional.AntiNoob.Settings antiNoob = config.ControllerMessages.TurnedFunc.AntiNoobSetting.AntiNoobPM;
            if (antiNoob.AntiNoobActivate)
                if (IsNoob(Sender.userID, antiNoob.TimeBlocked))
                {
                    ReplySystem(Sender, GetLang("IQCHAT_INFO_ANTI_NOOB_PM", Sender.UserIDString, FormatTime(UserInformationConnection[Sender.userID].LeftTime(antiNoob.TimeBlocked), Sender.UserIDString)));
                    return;
                }

            if (!PMHistory.ContainsKey(Sender))
            {
                ReplySystem(Sender, GetLang("COMMAND_R_NOTMSG", Sender.UserIDString));
                return;
            }

            BasePlayer RetargetUser = PMHistory[Sender];
            if (RetargetUser == null)
            {
                ReplySystem(Sender, GetLang("COMMAND_PM_NOT_USER", Sender.UserIDString));
                return;
            }
		   		 		  						  	   		  	  			  	  			  	 				  	  	
            User InfoRetarget = UserInformation[RetargetUser.userID];
            User InfoSender = UserInformation[RetargetUser.userID];

            if (!InfoRetarget.Settings.TurnPM)
            {
                ReplySystem(Sender, GetLang("FUNC_MESSAGE_PM_TURN_FALSE", Sender.UserIDString));
                return;
            }
            if (ControllerMessages.TurnedFunc.IgnoreUsePM)
            {
                if (InfoRetarget.Settings.IsIgnored(Sender.userID))
                {
                    ReplySystem(Sender, GetLang("IGNORE_NO_PM", Sender.UserIDString));
                    return;
                }
                if (InfoSender.Settings.IsIgnored(RetargetUser.userID))
                {
                    ReplySystem(Sender, GetLang("IGNORE_NO_PM_ME", Sender.UserIDString));
                    return;
                }
            }

            String Message = GetMessageInArgs(Sender, arg);
            if (Message == null || Message.Length <= 0)
            {
                ReplySystem(Sender, GetLang("COMMAND_PM_NOT_NULL_MSG", Sender.UserIDString));
                return;
            }
            if (Message.Length > 125) return;
            Message = Message.EscapeRichText();

            PMHistory[RetargetUser] = Sender;

            GeneralInformation.RenameInfo RenameSender = GeneralInfo.GetInfoRename(Sender.userID);
            GeneralInformation.RenameInfo RenamerTarget = GeneralInfo.GetInfoRename(RetargetUser.userID);
            String DisplayNameSender = RenameSender != null ? RenameSender.RenameNick ?? Sender.displayName : Sender.displayName;
            String TargetDisplayName = RenamerTarget != null ? RenamerTarget.RenameNick ?? RetargetUser.displayName : RetargetUser.displayName;
		   		 		  						  	   		  	  			  	  			  	 				  	  	
            ReplySystem(RetargetUser, GetLang("COMMAND_PM_SEND_MSG", RetargetUser.UserIDString, DisplayNameSender, Message));
            ReplySystem(Sender, GetLang("COMMAND_PM_SUCCESS", Sender.UserIDString, Message, TargetDisplayName));

            if (InfoRetarget.Settings.TurnSound)
                Effect.server.Run(ControllerMessages.TurnedFunc.PMSetting.SoundPM, RetargetUser.GetNetworkPosition());

            Log(LanguageEn ? $"PRIVATE MESSAGES : {Sender.displayName} sent a message to the player - {RetargetUser.displayName}\nMESSAGE : {Message}" : $"ЛИЧНЫЕ СООБЩЕНИЯ : {Sender.displayName} отправил сообщение игроку - {RetargetUser.displayName}\nСООБЩЕНИЕ : {Message}");
            DiscordLoggPM(Sender, RetargetUser, Message);

            RCon.Broadcast(RCon.LogType.Chat, new Chat.ChatEntry
            {
                Message = LanguageEn ? $"PRIVATE MESSAGES : {Sender.displayName}({Sender.userID}) -> {RetargetUser.displayName} : MESSAGE : {Message}" : $"ЛИЧНЫЕ СООБЩЕНИЯ : {Sender.displayName}({Sender.userID}) -> {RetargetUser.displayName} : СООБЩЕНИЕ : {Message}",
                UserId = Sender.UserIDString,
                Username = Sender.displayName,
                Channel = Chat.ChatChannel.Global,
                Time = (DateTime.UtcNow.Hour * 3600) + (DateTime.UtcNow.Minute * 60),
                Color = "#3f4bb8",
            });
            PrintWarning(LanguageEn ? $"PRIVATE MESSAGES : {Sender.displayName}({Sender.userID}) -> {RetargetUser.displayName} : MESSAGE : {Message}" : $"ЛИЧНЫЕ СООБЩЕНИЯ : {Sender.displayName}({Sender.userID}) -> {RetargetUser.displayName} : СООБЩЕНИЕ : {Message}");
        }

        private void DiscordLoggPM(BasePlayer Sender, BasePlayer Reciepter, String MessageLogged)
        {
            Configuration.OtherSettings.General PMChat = config.OtherSetting.LogsPMChat;
            if (!PMChat.UseLogged) return;

            GeneralInformation.RenameInfo SenderRename = GeneralInfo.GetInfoRename(Sender.userID);
            GeneralInformation.RenameInfo ReciepterRename = GeneralInfo.GetInfoRename(Reciepter.userID);

            UInt64 UserIDSender = SenderRename != null ? SenderRename.RenameID == 0 ? Sender.userID : SenderRename.RenameID : Sender.userID;
            UInt64 UserIDReciepter = ReciepterRename != null ? ReciepterRename.RenameID == 0 ? Reciepter.userID : ReciepterRename.RenameID : Reciepter.userID;
            String SenderName = SenderRename != null ? ReciepterRename.RenameNick ?? Sender.displayName : Sender.displayName;
            String ReciepterName = ReciepterRename != null ? ReciepterRename.RenameNick ?? Reciepter.displayName : Reciepter.displayName;
		   		 		  						  	   		  	  			  	  			  	 				  	  	
            List<Fields> fields = new List<Fields>
                        {
                            new Fields(LanguageEn ? "Sender" : "Отправитель", $"{SenderName}({UserIDSender})", true),
                            new Fields(LanguageEn ? "Recipient" : "Получатель", $"{ReciepterName}({UserIDReciepter})", true),
                            new Fields(LanguageEn ? "Message" : "Сообщение", MessageLogged, false),
                        };

            FancyMessage newMessage = new FancyMessage(null, false, new FancyMessage.Embeds[1] { new FancyMessage.Embeds(null, 16608621, fields, new Authors("IQChat PM-History", null, "https://i.imgur.com/xiwsg5m.png", null), null) });

            Request($"{PMChat.Webhooks}", newMessage.toJSON());
        }

        private static InterfaceBuilder _interface;
        public class FakePlayer
        {
            public string DisplayName;
            public ulong UserID;
        }
        private void DiscordLoggChat(BasePlayer player, Chat.ChatChannel Channel, String MessageLogged)
        {
            List<Fields> fields = new List<Fields>
                        {
                            new Fields(LanguageEn ? "Nick" : "Ник", player.displayName, true),
                            new Fields("Steam64ID", player.UserIDString, true),
                            new Fields(LanguageEn ? "Channel" : "Канал", Channel == Chat.ChatChannel.Global ? (LanguageEn ? "Global" : "Глобальный чат") : Channel == Chat.ChatChannel.Cards ? (LanguageEn ? "Poker" : "Покерный чат") : (LanguageEn ? "Team" : "Командный чат"), true),
                            new Fields(LanguageEn ? "Message" : "Сообщение", MessageLogged, false),
                        };

            FancyMessage newMessage = new FancyMessage(null, false, new FancyMessage.Embeds[1] { new FancyMessage.Embeds(null, 10710525, fields, new Authors("IQChat Chat-History", null, "https://i.imgur.com/xiwsg5m.png", null), null) });

            switch (Channel)
            {
                case Chat.ChatChannel.Cards:
                case Chat.ChatChannel.Global:
                    {
                        Configuration.OtherSettings.General GlobalChat = config.OtherSetting.LogsChat.GlobalChatSettings;
                        if (!GlobalChat.UseLogged) return;
                        Request($"{GlobalChat.Webhooks}", newMessage.toJSON());
                        break;
                    }
                case Chat.ChatChannel.Team:
                    {
                        Configuration.OtherSettings.General TeamChat = config.OtherSetting.LogsChat.TeamChatSettings;
                        if (!TeamChat.UseLogged) return;
                        Request($"{TeamChat.Webhooks}", newMessage.toJSON());
                    }
                    break;
                default:
                    break;
            }
        }
        String API_GET_DEFAULT_PREFIX() => config.ControllerConnect.SetupDefaults.PrefixDefault;
        protected override void SaveConfig() => Config.WriteObject(config);
        [ChatCommand("alertuip")]
        private void AlertUIPChatCommand(BasePlayer Sender, String cmd, String[] args)
        {
            if (!permission.UserHasPermission(Sender.UserIDString, PermissionAlert)) return;
            if (args == null || args.Length == 0)
            {
                ReplySystem(Sender, LanguageEn ? "You didn't specify a player!" : "Вы не указали игрока!");
                return;
            }
            BasePlayer Recipient = BasePlayer.Find(args[0]);
            if (Recipient == null)
            {
                ReplySystem(Sender, LanguageEn ? "The player is not on the server!" : "Игрока нет на сервере!");
                return;
            }
            AlertUI(Sender, Recipient, args.Skip(1).ToArray());
        }

        private const String PermissionHideOnline = "iqchat.onlinehide";

        
                void AlertUI(BasePlayer Sender, string[] arg)
        {
            if (_interface == null)
            {
                PrintWarning(LanguageEn ? "We generate the interface, wait for a message about successful generation" : "Генерируем интерфейс, ожидайте сообщения об успешной генерации");
                return;
            }
            String Message = GetMessageInArgs(Sender, arg);
            if (Message == null) return;

            foreach (BasePlayer PlayerInList in BasePlayer.activePlayerList)
                DrawUI_IQChat_Alert(PlayerInList, Message);
        }
        
                public void RemoveReserved(UInt64 userID)
        {
            if (!IQFakeActive) return;
            IQFakeActive?.Call("RemoveReserver", userID);
        }
		   		 		  						  	   		  	  			  	  			  	 				  	  	
        
                public Boolean IsNoob(UInt64 userID, Int32 TimeBlocked)
        {
            if (UserInformationConnection.ContainsKey(userID))
                return UserInformationConnection[userID].IsNoob(TimeBlocked);
            return false;
        }
        void API_ALERT(String Message, Chat.ChatChannel channel = Chat.ChatChannel.Global, String CustomPrefix = null, String CustomAvatar = null, String CustomHex = null)
        {
            foreach (BasePlayer p in BasePlayer.activePlayerList)
                ReplySystem(p, Message, CustomPrefix, CustomAvatar, CustomHex);
        }
        Boolean API_CHECK_MUTE_CHAT(UInt64 ID)
        {
            if (!UserInformation.ContainsKey(ID)) return false;
            return UserInformation[ID].MuteInfo.IsMute(MuteType.Chat);
        }
        void Unload()
        {
            InterfaceBuilder.DestroyAll();

            WriteData();
            _ = null;
        }
        
        void ReplyBroadcast(String CustomPrefix = null, String CustomAvatar = null, Boolean AdminAlert = false, String LangKey = "", params object[] args)
        {
            foreach (BasePlayer p in !AdminAlert ? BasePlayer.activePlayerList.Where(p => UserInformation[p.userID].Settings.TurnBroadcast) : BasePlayer.activePlayerList)
                ReplySystem(p,GetLang(LangKey, p.UserIDString, args), CustomPrefix, CustomAvatar);
        }

        [ConsoleCommand("unmute")]
        void UnMuteCustomAdmin(ConsoleSystem.Arg arg)
        {
            if (arg.Player() != null)
                if (!permission.UserHasPermission(arg.Player().UserIDString, PermissionMute)) return;

            if (arg?.Args == null || arg.Args.Length != 1 || arg.Args.Length > 1)
            {
                PrintWarning(LanguageEn ? "Invalid syntax, please use : unmute Steam64ID" : "Неверный синтаксис,используйте : unmute Steam64ID");
                return;
            }

            string NameOrID = arg.Args[0];
            BasePlayer target = GetPlayerNickOrID(NameOrID);

            if (target == null)
            {
                UInt64 Steam64ID = 0;
                if (UInt64.TryParse(NameOrID, out Steam64ID))
                {
                    if (UserInformation.ContainsKey(Steam64ID))
                    {
                        User Info = UserInformation[Steam64ID];
                        if (Info == null) return;
                        if (!Info.MuteInfo.IsMute(MuteType.Chat))
                        {
                            ConsoleOrPrintMessage(arg.Player(),
                                LanguageEn ? "The player does not have a chat lock" : "У игрока нет блокировки чата");
                            return;
                        }

                        Info.MuteInfo.UnMute(MuteType.Chat);

                        ConsoleOrPrintMessage(arg.Player(),
                            LanguageEn ? "You have unblocked the offline chat to the player" : "Вы разблокировали чат offline игроку");
                        return;
                    }
                    else
                    {
                        ConsoleOrPrintMessage(arg.Player(),
                            LanguageEn ? "This player is not on the server" : "Такого игрока нет на сервере");
                        return;
                    }
                }
                else
                {
                    ConsoleOrPrintMessage(arg.Player(),
                        LanguageEn ? "This player is not on the server" : "Такого игрока нет на сервере");
                    return;
                }
            }

            UnmutePlayer(target, MuteType.Chat, arg.Player(), false, true);
            Puts(LanguageEn ? "Successfully" : "Успешно");
        }

        
                
        private void Log(String LoggedMessage) => LogToFile("IQChatLogs", LoggedMessage, this);

        [ChatCommand("alertui")]
        private void AlertUIChatCommand(BasePlayer Sender, String cmd, String[] args)
        {
            if (!permission.UserHasPermission(Sender.UserIDString, PermissionAlert)) return;
            AlertUI(Sender, args);
        }
        private void DrawUI_IQChat_Mute_And_Ignore_Pages(BasePlayer player, Boolean IsNextPage, SelectedAction Action, Int32 Page = 0)
        {
            String Interface = InterfaceBuilder.GetInterface("UI_Chat_Mute_And_Ignore_Pages");
            if (Interface == null) return;
		   		 		  						  	   		  	  			  	  			  	 				  	  	
            String CommandRight = IsNextPage ? $"newui.cmd action.mute.ignore page.controller {Action} {Page + 1}" : String.Empty;
            String ColorRight = String.IsNullOrEmpty(CommandRight) ? "1 1 1 0.1" : "1 1 1 1";

            String CommandLeft = Page > 0 ? $"newui.cmd action.mute.ignore page.controller {Action} {Page - 1}" : String.Empty;
            String ColorLeft = String.IsNullOrEmpty(CommandLeft) ? "1 1 1 0.1" : "1 1 1 1";
		   		 		  						  	   		  	  			  	  			  	 				  	  	
            Interface = Interface.Replace("%COMMAND_LEFT%", CommandLeft);
            Interface = Interface.Replace("%COMMAND_RIGHT%", CommandRight);
            Interface = Interface.Replace("%PAGE%", $"{Page}");
            Interface = Interface.Replace("%COLOR_LEFT%", ColorLeft);
            Interface = Interface.Replace("%COLOR_RIGHT%", ColorRight);

            CuiHelper.DestroyUi(player, "PageCount");
            CuiHelper.DestroyUi(player, "LeftPage");
            CuiHelper.DestroyUi(player, "RightPage");
            CuiHelper.AddUi(player, Interface);
        }

        [ConsoleCommand("hmute")]
        void HideMuteConsole(ConsoleSystem.Arg arg)
        {
            if (arg.Player() != null)
                if (!permission.UserHasPermission(arg.Player().UserIDString, PermissionMute)) return;

            if (arg == null || arg.Args == null || arg.Args.Length != 3 || arg.Args.Length > 3)
            {
                ConsoleOrPrintMessage(arg.Player(),
                    LanguageEn
                        ? "Invalid syntax, use : hmute Steam64ID Reason Time (seconds)"
                        : "Неверный синтаксис,используйте : hmute Steam64ID Причина Время(секунды)");
                return;
            }
            string NameOrID = arg.Args[0];
            string Reason = arg.Args[1];
            Int32 TimeMute = 0;
            if (!Int32.TryParse(arg.Args[2], out TimeMute))
            {
                ConsoleOrPrintMessage(arg.Player(),
                    LanguageEn ? "Enter the time in numbers!" : "Введите время цифрами!");
                return;
            }
            BasePlayer target = GetPlayerNickOrID(NameOrID);
            if (target == null)
            {
                UInt64 Steam64ID = 0;
                if (UInt64.TryParse(NameOrID, out Steam64ID))
                {
                    if (UserInformation.ContainsKey(Steam64ID))
                    {
                        User Info = UserInformation[Steam64ID];
                        if (Info == null) return;
                        if (Info.MuteInfo.IsMute(MuteType.Chat))
                        {
                            ConsoleOrPrintMessage(arg.Player(),
                                LanguageEn ? "The player already has a chat lock" : "Игрок уже имеет блокировку чата");
                            return;
                        }

                        Info.MuteInfo.SetMute(MuteType.Chat, TimeMute);

                        ConsoleOrPrintMessage(arg.Player(),
                            LanguageEn ? "Chat blocking issued to offline player" : "Блокировка чата выдана offline-игроку");
                        return;
                    }
                    else
                    {
                        ConsoleOrPrintMessage(arg.Player(),
                            LanguageEn ? "This player is not on the server" : "Такого игрока нет на сервере");
                        return;
                    }
                }
                else
                {
                    ConsoleOrPrintMessage(arg.Player(),
                        LanguageEn ? "This player is not on the server" : "Такого игрока нет на сервере");
                    return;
                }
            }

            MutePlayer(target, MuteType.Chat, 0, arg.Player(), Reason, TimeMute, true, true);
        }
        class Response
        {
            [JsonProperty("country")]
            public string Country { get; set; }
        }

        void ReplySystem(BasePlayer player, String Message, String CustomPrefix = null, String CustomAvatar = null, String CustomHex = null)
        {
            Configuration.ControllerMessage ControllerMessages = config.ControllerMessages;

            String Prefix = (CustomPrefix == null || String.IsNullOrWhiteSpace(CustomPrefix)) ? (ControllerMessages.GeneralSetting.BroadcastFormat.BroadcastTitle == null || String.IsNullOrWhiteSpace(ControllerMessages.GeneralSetting.BroadcastFormat.BroadcastTitle)) ? "" : ControllerMessages.GeneralSetting.BroadcastFormat.BroadcastTitle : CustomPrefix;
            String AvatarID = (CustomAvatar == null || String.IsNullOrWhiteSpace(CustomAvatar)) ? (ControllerMessages.GeneralSetting.BroadcastFormat.Steam64IDAvatar == null || String.IsNullOrWhiteSpace(ControllerMessages.GeneralSetting.BroadcastFormat.Steam64IDAvatar)) ? "0" : ControllerMessages.GeneralSetting.BroadcastFormat.Steam64IDAvatar : CustomAvatar;
            String Hex = (CustomHex == null || String.IsNullOrWhiteSpace(CustomHex)) ? (ControllerMessages.GeneralSetting.BroadcastFormat.BroadcastColor == null || String.IsNullOrWhiteSpace(ControllerMessages.GeneralSetting.BroadcastFormat.BroadcastColor)) ? "#ffff" : ControllerMessages.GeneralSetting.BroadcastFormat.BroadcastColor : CustomHex;
           
            player.SendConsoleCommand("chat.add", Chat.ChatChannel.Global, AvatarID, $"{Prefix}<color={Hex}>{Message}</color>");
        }

        protected override void LoadDefaultConfig() => config = Configuration.GetNewConfiguration();
        private void MigrateDataToNoob()
        {
            if (config.ControllerMessages.TurnedFunc.AntiNoobSetting.AntiNoobPM.AntiNoobActivate || config.ControllerMessages.TurnedFunc.AntiNoobSetting.AntiNoobChat.AntiNoobActivate)
            {
                if (UserInformationConnection.Count == 0 || UserInformationConnection == null)
                {
                    PrintWarning(LanguageEn ? "Migration of old players to Anti-Nub.." : "Миграция старых игроков в Анти-Нуб..");
                    foreach (KeyValuePair<UInt64, User> InfoUser in UserInformation.Where(x => !UserInformationConnection.ContainsKey(x.Key)))
                        UserInformationConnection.Add(InfoUser.Key, new AntiNoob { DateConnection = new DateTime(2022, 1, 1) });
                    PrintWarning(LanguageEn ? "Migration of old players completed" : "Миграция старых игроков завершена");
                }
            }
        }
        public bool IsFake(String DisplayName)
        {
            if (!IQFakeActive) return false;

            return (bool)IQFakeActive?.Call("IsFake", DisplayName);
        }
        Boolean API_CHECK_VOICE_CHAT(UInt64 ID)
        {
            if (!UserInformation.ContainsKey(ID)) return false;
            return UserInformation[ID].MuteInfo.IsMute(MuteType.Voice);
        }

        
                public GeneralInformation GeneralInfo = new GeneralInformation();
        private void SeparatorChat(Chat.ChatChannel channel, BasePlayer player, String Message)
        {
            Configuration.ControllerMessage.TurnedFuncional.AntiNoob.Settings antiNoob = config.ControllerMessages.TurnedFunc.AntiNoobSetting.AntiNoobChat;
            if (antiNoob.AntiNoobActivate)
                if (IsNoob(player.userID, antiNoob.TimeBlocked))
                {
                    ReplySystem(player, GetLang("IQCHAT_INFO_ANTI_NOOB", player.UserIDString, FormatTime(UserInformationConnection[player.userID].LeftTime(antiNoob.TimeBlocked), player.UserIDString)));
                    return;
                }

            Configuration.ControllerMessage ControllerMessage = config.ControllerMessages;
            User Info = UserInformation[player.userID];

            if (ControllerMessage.TurnedFunc.AntiSpamSetting.AntiSpamActivate)
                if (!permission.UserHasPermission(player.UserIDString, PermissionAntiSpam))
                {
                    if (!Info.MuteInfo.IsMute(MuteType.Chat))
                    {
                        if (!Flooders.ContainsKey(player.userID))
                            Flooders.Add(player.userID, new FlooderInfo { Time = CurrentTime + ControllerMessage.TurnedFunc.AntiSpamSetting.FloodTime, LastMessage = Message });
                        else
                        {
                            if (Flooders[player.userID].Time > CurrentTime)
                            {
                                ReplySystem(player, GetLang("FLOODERS_MESSAGE", player.UserIDString, Convert.ToInt32(Flooders[player.userID].Time - CurrentTime)));
                                return;
                            }

                            if (ControllerMessage.TurnedFunc.AntiSpamSetting.AntiSpamDuplesSetting.AntiSpamDuplesActivate)
                            {
                                if (Flooders[player.userID].LastMessage == Message)
                                {
                                    if (Flooders[player.userID].TryFlood >= ControllerMessage.TurnedFunc.AntiSpamSetting.AntiSpamDuplesSetting.TryDuples)
                                    {
                                        MutePlayer(player, MuteType.Chat, 0, null, ControllerMessage.TurnedFunc.AntiSpamSetting.AntiSpamDuplesSetting.MuteSetting.Reason, ControllerMessage.TurnedFunc.AntiSpamSetting.AntiSpamDuplesSetting.MuteSetting.SecondMute);
                                        Flooders[player.userID].TryFlood = 0;
                                        return;
                                    }
                                    Flooders[player.userID].TryFlood++;
                                }
                            }
                        }
                        Flooders[player.userID].Time = ControllerMessage.TurnedFunc.AntiSpamSetting.FloodTime + CurrentTime;
                        Flooders[player.userID].LastMessage = Message;
                    }
                }

            GeneralInformation General = GeneralInfo;
            GeneralInformation.RenameInfo RenameInformation = General.GetInfoRename(player.userID);
		   		 		  						  	   		  	  			  	  			  	 				  	  	
            Configuration.ControllerParameters ControllerParameter = config.ControllerParameter;
            Configuration.ControllerMute ControllerMutes = config.ControllerMutes;
            Configuration.ControllerMessage.GeneralSettings.OtherSettings OtherController = config.ControllerMessages.GeneralSetting.OtherSetting;

            if (General.TurnMuteAllChat)
            {
                ReplySystem(player, GetLang("IQCHAT_FUNCED_NO_SEND_CHAT_MUTED_ALL_CHAT", player.UserIDString));
                return;
            }
		   		 		  						  	   		  	  			  	  			  	 				  	  	
            if (channel == Chat.ChatChannel.Team && !ControllerMessage.TurnedFunc.MuteTeamChat) { }
            else if (Info.MuteInfo.IsMute(MuteType.Chat))
            {
                ReplySystem(player,
                    GetLang("IQCHAT_FUNCED_NO_SEND_CHAT_MUTED", player.UserIDString,
                        FormatTime(Info.MuteInfo.GetTime(MuteType.Chat), player.UserIDString)));
                return;
            }

            String Prefixes = String.Empty;
            String FormattingMessage = Message;
            String DisplayName = player.displayName;

            UInt64 UserID = player.userID;
            if (RenameInformation != null)
            {
                DisplayName = RenameInformation.RenameNick;
                UserID = RenameInformation.RenameID;
            }

            String ColorNickPlayer = String.IsNullOrWhiteSpace(Info.Info.ColorNick) ? player.IsAdmin ? "#a8fc55" : "#54aafe" : Info.Info.ColorNick;
            DisplayName = $"<color={ColorNickPlayer}>{DisplayName}</color>";

            //channel == Chat.ChatChannel.Team ? "<color=#a5e664>[Team]</color>" : 
            String ChannelMessage = channel == Chat.ChatChannel.Cards ? "<color=#AA8234>[Cards]</color>" :  channel == Chat.ChatChannel.Clan ? "<color=#a5e664>[Clan]</color>" : "";

            if (ControllerMessage.Formatting.UseBadWords)
            {
                Tuple<String, Boolean> GetTuple = BadWordsCleaner(Message, ControllerMessage.Formatting.ReplaceBadWord, ControllerMessage.Formatting.BadWords);
                FormattingMessage = GetTuple.Item1;

                if (GetTuple.Item2 && channel == Chat.ChatChannel.Global)
                {
                    if (permission.UserHasPermission(player.UserIDString, PermissionMute))
                        Interface.Oxide.CallHook("OnModeratorSendBadWords", player, GetTuple.Item1);
		   		 		  						  	   		  	  			  	  			  	 				  	  	
                    Interface.Oxide.CallHook("OnPlayerSendBadWords", player, GetTuple.Item1);

                    if (ControllerMutes.AutoMuteSettings.UseAutoMute)
                        MutePlayer(player, MuteType.Chat, 0, null, ControllerMutes.AutoMuteSettings.AutoMuted.Reason, ControllerMutes.AutoMuteSettings.AutoMuted.SecondMute);
                }
            }

            if (ControllerMessage.Formatting.FormatMessage)
                FormattingMessage = $"{FormattingMessage.Substring(0, 1).ToUpper()}{FormattingMessage.Remove(0, 1).ToLower()}";

            if (ControllerParameter.Prefixes.TurnMultiPrefixes)
            {
                if (Info.Info.PrefixList != null)
                    Prefixes = String.Join("", Info.Info.PrefixList.Take(ControllerParameter.Prefixes.MaximumMultiPrefixCount));
            }
            else Prefixes = Info.Info.Prefix;
            
            String ResultMessage = String.IsNullOrWhiteSpace(Info.Info.ColorMessage) ? FormattingMessage : $"<color={Info.Info.ColorMessage}>{FormattingMessage}</color>";;

            String ResultReference = GetReferenceTags(player); 
            String SendFormat = $"{ChannelMessage} {ResultReference}<size={OtherController.SizePrefix}>{Prefixes}</size> <size={OtherController.SizeNick}>{DisplayName}</size>";
            
            if (config.RustPlusSettings.UseRustPlus)
                if (channel == Chat.ChatChannel.Team)
                {
                    RelationshipManager.PlayerTeam Team = RelationshipManager.ServerInstance.FindTeam(player.currentTeam);
                    if (Team == null) return;
                    Util.BroadcastTeamChat(player.Team, player.userID, player.displayName, FormattingMessage, Info.Info.ColorMessage);
                }

            if (ControllerMutes.LoggedMute.UseHistoryMessage && config.OtherSetting.LogsMuted.UseLogged)
                AddHistoryMessage(player, FormattingMessage);

            ReplyChat(channel, player, ResultMessage, SendFormat);
            AnwserMessage(player, ResultMessage.ToLower());
            Puts($"{player.displayName}({player.UserIDString}): {FormattingMessage}");
            Log(LanguageEn ? $"CHAT MESSAGE : {player}: {ChannelMessage} {FormattingMessage}" : $"СООБЩЕНИЕ В ЧАТ : {player}: {ChannelMessage} {FormattingMessage}");
            DiscordLoggChat(player, channel, Message);

            RCon.Broadcast(RCon.LogType.Chat, new Chat.ChatEntry
            {
                Message = $"{player.displayName} : {FormattingMessage}",
                UserId = player.UserIDString,
                Username = player.displayName,
                Channel = channel,
                Time = (DateTime.UtcNow.Hour * 3600) + (DateTime.UtcNow.Minute * 60),
            });
        }
        String API_GET_NICK_COLOR(ulong ID)
        {
            if (!UserInformation.ContainsKey(ID)) return String.Empty;

            return UserInformation[ID].Info.ColorNick;
        }
        void Alert(BasePlayer Sender, string[] arg, Boolean IsAdmin)
        {
            String Message = GetMessageInArgs(Sender, arg);
            if (Message == null) return;
		   		 		  						  	   		  	  			  	  			  	 				  	  	
            ReplyBroadcast(Message, AdminAlert: IsAdmin);

            if (config.RustPlusSettings.UseRustPlus)
                foreach (BasePlayer playerList in BasePlayer.activePlayerList)
                    NotificationList.SendNotificationTo(playerList.userID, NotificationChannel.SmartAlarm, config.RustPlusSettings.DisplayNameAlert, Message, Util.GetServerPairingData());
        }
        static Double CurrentTime => Facepunch.Math.Epoch.Current;
        String API_GET_PREFIX(UInt64 ID)
        {
            if (!UserInformation.ContainsKey(ID)) return String.Empty;
            Configuration.ControllerParameters ControllerParameter = config.ControllerParameter;

            User Info = UserInformation[ID];
            String Prefixes = String.Empty;

            if (ControllerParameter.Prefixes.TurnMultiPrefixes)
                Prefixes = String.Join("", Info.Info.PrefixList.Take(ControllerParameter.Prefixes.MaximumMultiPrefixCount));
            else Prefixes = Info.Info.Prefix;

            return Prefixes;
        }

        private void DiscordLoggMuted(BasePlayer Target, MuteType Type, String Reason = null, String TimeBlocked = null, BasePlayer Moderator = null)
        {
            Configuration.OtherSettings.General MuteChat = config.OtherSetting.LogsMuted;
            if (!MuteChat.UseLogged) return;

            Configuration.ControllerMute.LoggedFuncion ControllerMuted = config.ControllerMutes.LoggedMute;
		   		 		  						  	   		  	  			  	  			  	 				  	  	
            String ActionReason = String.Empty;

            GeneralInformation.RenameInfo RenameSender = GeneralInfo.GetInfoRename(Target.userID);

            UInt64 UserIDModeration = 0;
            String NickModeration = GetLang("IQCHAT_FUNCED_ALERT_TITLE_SERVER", Target.UserIDString);
            if (Moderator != null)
            {
                GeneralInformation.RenameInfo RenameModerator = GeneralInfo.GetInfoRename(Moderator.userID);

                UserIDModeration = RenameModerator != null ? RenameModerator.RenameID == 0 ? Moderator.userID : RenameModerator.RenameID : Moderator.userID;
                NickModeration = RenameModerator != null ? $"{RenameModerator.RenameNick ?? Moderator.displayName}" : Moderator.displayName;
            }

            String NickTarget = RenameSender != null ? $"{RenameSender.RenameNick ?? Target.displayName}" : Target.displayName;
            UInt64 UserIDTarget = RenameSender != null ? RenameSender.RenameID == 0 ? Target.userID : RenameSender.RenameID : Target.userID;

            List<Fields> fields;

            switch (Type)
            {
                case MuteType.Chat:
                    {
                        if (Reason != null)
                            ActionReason = LanguageEn ? "Mute chat" : "Блокировка чата";
                        else ActionReason = LanguageEn ? "Unmute chat" : "Разблокировка чата";
                        break;
                    }
                case MuteType.Voice:
                    {
                        if (Reason != null)
                            ActionReason = LanguageEn ? "Mute voice" : "Блокировка голоса";
                        else ActionReason = LanguageEn ? "Unmute voice" : "Разблокировка голоса";
                        break;
                    }
                default:
                    break;
            }
            Int32 Color = 0;
            if (Reason != null)
            {
                fields = new List<Fields>
                        {
                            new Fields(LanguageEn ? "Nickname of the moderator" : "Ник модератора", NickModeration, true),
                            new Fields(LanguageEn ? "Steam64ID Moderator" : "Steam64ID модератора", $"{UserIDModeration}", true),
                            new Fields(LanguageEn ? "Action" : "Действие", ActionReason, false),
                            new Fields(LanguageEn ? "Reason" : "Причина", Reason, false),
                            new Fields(LanguageEn ? "Time" : "Время", TimeBlocked, false),
                            new Fields(LanguageEn ? "Nick blocked" : "Ник заблокированного", NickTarget, true),
                            new Fields(LanguageEn ? "Steam64ID blocked" : "Steam64ID заблокированного", $"{UserIDTarget}", true),
                        };



                if (ControllerMuted.UseHistoryMessage)
                {
                    String Messages = GetLastMessage(Target, ControllerMuted.CountHistoryMessage);
                    if (Messages != null && !String.IsNullOrWhiteSpace(Messages))
                        fields.Insert(fields.Count, new Fields(LanguageEn ? $"The latter {ControllerMuted.CountHistoryMessage} messages" : $"Последние {ControllerMuted.CountHistoryMessage} сообщений", Messages, false));
                }

                Color = 14357781;
            }
            else
            {
                fields = new List<Fields>
                        {
                            new Fields(LanguageEn ? "Nickname of the moderator" : "Ник модератора", NickModeration, true),
                            new Fields(LanguageEn ? "Steam64ID moderator" : "Steam64ID модератора", $"{UserIDModeration}", true),
                            new Fields(LanguageEn ? "Action" : "Действие", ActionReason, false),
                            new Fields(LanguageEn ? "Nick blocked" : "Ник заблокированного", NickTarget, true),
                            new Fields(LanguageEn ? "Steam64ID blocked" : "Steam64ID заблокированного", $"{UserIDTarget}", true),
                        };
                Color = 1432346;
            }


            FancyMessage newMessage = new FancyMessage(null, false, new FancyMessage.Embeds[1] { new FancyMessage.Embeds(null, Color, fields, new Authors("IQChat Mute-History", null, "https://i.imgur.com/xiwsg5m.png", null), null) });

            Request($"{MuteChat.Webhooks}", newMessage.toJSON());
        }
        void API_SEND_PLAYER_CONNECTED(BasePlayer player, String DisplayName, String country, String userID)
        {
            Configuration.ControllerAlert.PlayerSession AlertSessionPlayer = config.ControllerAlertSetting.PlayerSessionSetting;

            if (AlertSessionPlayer.ConnectedAlert)
            {
                String Avatar = AlertSessionPlayer.ConnectedAvatarUse ? userID : String.Empty;
                if (AlertSessionPlayer.ConnectedWorld)
                    ReplyBroadcast(null, Avatar, false, "WELCOME_PLAYER_WORLD", DisplayName, country);
                    //ReplyBroadcast(GetLang("WELCOME_PLAYER_WORLD", player.UserIDString, DisplayName, country), CustomAvatar: Avatar);
                else ReplyBroadcast(null, Avatar, false, "WELCOME_PLAYER", DisplayName);
                    //ReplyBroadcast(GetLang("WELCOME_PLAYER", player.UserIDString, DisplayName), CustomAvatar: Avatar);
            }
        }
        public Dictionary<BasePlayer, List<String>> LastMessagesChat = new Dictionary<BasePlayer, List<String>>();
        private void DrawUI_IQChat_Slider_Update_Argument(BasePlayer player, TakeElementUser ElementType)
        {
            String Interface = InterfaceBuilder.GetInterface("UI_Chat_Slider_Update_Argument");
            User Info = UserInformation[player.userID];
            if (Info == null || Interface == null) return;

            String Argument = String.Empty;
            String Name = String.Empty;
            String Parent = String.Empty;

            switch (ElementType)
            {
                case TakeElementUser.Prefix:
                    Argument = Info.Info.Prefix;
                    Parent = "SLIDER_PREFIX";
                    Name = "ARGUMENT_PREFIX";
                    break;
                case TakeElementUser.Nick:
                    Argument = $"<color={Info.Info.ColorNick}>{player.displayName}</color>";
                    Parent = "SLIDER_NICK_COLOR";
                    Name = "ARGUMENT_NICK_COLOR";
                    break;
                case TakeElementUser.Chat:
                    Argument = $"<color={Info.Info.ColorMessage}>{GetLang("IQCHAT_CONTEXT_NICK_DISPLAY_MESSAGE", player.UserIDString)}</color>";
                    Parent = "SLIDER_MESSAGE_COLOR";
                    Name = "ARGUMENT_MESSAGE_COLOR";
                    break;
                case TakeElementUser.Rank:
                    Argument = IQRankGetNameRankKey(Info.Info.Rank) ?? GetLang("IQCHAT_CONTEXT_SLIDER_IQRANK_TITLE_NULLER", player.UserIDString);
                    Parent = "SLIDER_IQRANK";
                    Name = "ARGUMENT_RANK";
                    break;
                default:
                    break;
            }

            String Pattern = @"</?size.*?>";
            String ArgumentRegex = Regex.IsMatch(Argument, Pattern) ? Regex.Replace(Argument, Pattern, "") : Argument;
            Interface = Interface.Replace("%ARGUMENT%", ArgumentRegex);
            Interface = Interface.Replace("%PARENT%", Parent);
            Interface = Interface.Replace("%NAME%", Name);

            CuiHelper.DestroyUi(player, Name);
            CuiHelper.AddUi(player, Interface);

        }
        private class Configuration
        {
                        [JsonProperty(LanguageEn ? "Setting up player information" : "Настройка информации о игроке")]
            public ControllerConnection ControllerConnect = new ControllerConnection();
            internal class ControllerConnection
            {
                [JsonProperty(LanguageEn ? "Function switches" : "Перключатели функций")]
                public Turned Turneds = new Turned();
                [JsonProperty(LanguageEn ? "Setting Standard Values" : "Настройка стандартных значений")]
                public SetupDefault SetupDefaults = new SetupDefault();

                internal class SetupDefault
                {
                    [JsonProperty(LanguageEn ? "This prefix will be set if the player entered the server for the first time or in case of expiration of the rights to the prefix that he had earlier" : "Данный префикс установится если игрок впервые зашел на сервер или в случае окончания прав на префикс, который у него стоял ранее")]
                    public String PrefixDefault = "<color=#CC99FF>[ИГРОК]</color>";
                    [JsonProperty(LanguageEn ? "This nickname color will be set if the player entered the server for the first time or in case of expiration of the rights to the nickname color that he had earlier" : "Данный цвет ника установится если игрок впервые зашел на сервер или в случае окончания прав на цвет ника, который у него стоял ранее")]
                    public String NickDefault = "#33CCCC";
                    [JsonProperty(LanguageEn ? "This chat color will be set if the player entered the server for the first time or in case of expiration of the rights to the chat color that he had earlier" : "Данный цвет чата установится если игрок впервые зашел на сервер или в случае окончания прав на цвет чата, который у него стоял ранее")]
                    public String MessageDefault = "#0099FF";
                }
                internal class Turned
                {
                    [JsonProperty(LanguageEn ? "Set automatically a prefix to a player when he got the rights to it" : "Устанавливать автоматически префикс игроку, когда он получил права на него")]
                    public Boolean TurnAutoSetupPrefix;
                    [JsonProperty(LanguageEn ? "Set automatically the color of the nickname to the player when he got the rights to it" : "Устанавливать автоматически цвет ника игроку, когда он получил права на него")]
                    public Boolean TurnAutoSetupColorNick;
                    [JsonProperty(LanguageEn ? "Set the chat color automatically to the player when he got the rights to it" : "Устанавливать автоматически цвет чата игроку, когда он получил права на него")]
                    public Boolean TurnAutoSetupColorChat;
                    [JsonProperty(LanguageEn ? "Automatically reset the prefix when the player's rights to it expire" : "Сбрасывать автоматически префикс при окончании прав на него у игрока")]
                    public Boolean TurnAutoDropPrefix;
                    [JsonProperty(LanguageEn ? "Automatically reset the color of the nickname when the player's rights to it expire" : "Сбрасывать автоматически цвет ника при окончании прав на него у игрока")]
                    public Boolean TurnAutoDropColorNick;
                    [JsonProperty(LanguageEn ? "Automatically reset the color of the chat when the rights to it from the player expire" : "Сбрасывать автоматически цвет чата при окончании прав на него у игрока")]
                    public Boolean TurnAutoDropColorChat;
                }
            }
            
                        [JsonProperty(LanguageEn ? "Setting options for the player" : "Настройка параметров для игрока")]
            public ControllerParameters ControllerParameter = new ControllerParameters();
            internal class ControllerParameters
            {
                [JsonProperty(LanguageEn ? "Setting the display of options for player selection" : "Настройка отображения параметров для выбора игрока")]
                public VisualSettingParametres VisualParametres = new VisualSettingParametres();
                [JsonProperty(LanguageEn ? "List and customization of colors for a nickname" : "Список и настройка цветов для ника")]
                public List<AdvancedFuncion> NickColorList = new List<AdvancedFuncion>();
                [JsonProperty(LanguageEn ? "List and customize colors for chat messages" : "Список и настройка цветов для сообщений в чате")]
                public List<AdvancedFuncion> MessageColorList = new List<AdvancedFuncion>();
                [JsonProperty(LanguageEn ? "List and configuration of prefixes in chat" : "Список и настройка префиксов в чате")]
                public PrefixSetting Prefixes = new PrefixSetting();
                internal class PrefixSetting
                {
                    [JsonProperty(LanguageEn ? "Enable support for multiple prefixes at once (true - multiple prefixes can be set/false - only 1 can be set to choose from)" : "Включить поддержку нескольких префиксов сразу (true - можно установить несколько префиксов/false - установить можно только 1 на выбор)")]
                    public Boolean TurnMultiPrefixes;
                    [JsonProperty(LanguageEn ? "The maximum number of prefixes that can be set at a time (This option only works if setting multiple prefixes is enabled)" : "Максимальное количество префиксов, которое можно установить за раз(Данный параметр работает только если включена установка нескольких префиксов)")]
                    public Int32 MaximumMultiPrefixCount;
                    [JsonProperty(LanguageEn ? "List of prefixes and their settings" : "Список префиксов и их настройка")]
                    public List<AdvancedFuncion> Prefixes = new List<AdvancedFuncion>();
                }

                internal class AdvancedFuncion
                {
                    [JsonProperty(LanguageEn ? "Permission" : "Права")]
                    public String Permissions;
                    [JsonProperty(LanguageEn ? "Argument" : "Значение")]
                    public String Argument;
                    [JsonProperty(LanguageEn ? "Block the player's ability to select this parameter in the plugin menu (true - yes/false - no)" : "Заблокировать возможность выбрать данный параметр игроком в меню плагина (true - да/false - нет)")]
                    public Boolean IsBlockSelected;
                }

                internal class VisualSettingParametres
                {
                    [JsonProperty(LanguageEn ? "Player prefix selection display type - (0 - dropdown list, 1 - slider (Please note that if you have multi-prefix enabled, the dropdown list will be set))" : "Тип отображения выбора префикса для игрока - (0 - выпадающий список, 1 - слайдер (Учтите, что если у вас включен мульти-префикс, будет установлен выпадающий список))")]
                    public SelectedParametres PrefixType;
                    [JsonProperty(LanguageEn ? "Display type of player's nickname color selection - (0 - drop-down list, 1 - slider)" : "Тип отображения выбора цвета ника для игрока - (0 - выпадающий список, 1 - слайдер)")]
                    public SelectedParametres NickColorType;
                    [JsonProperty(LanguageEn ? "Display type of message color choice for the player - (0 - drop-down list, 1 - slider)" : "Тип отображения выбора цвета сообщения для игрока - (0 - выпадающий список, 1 - слайдер)")]
                    public SelectedParametres ChatColorType;
                    [JsonProperty(LanguageEn ? "IQRankSystem : Player rank selection display type - (0 - drop-down list, 1 - slider)" : "IQRankSystem : Тип отображения выбора ранга для игрока - (0 - выпадающий список, 1 - слайдер)")]
                    public SelectedParametres IQRankSystemType;
                }
            }
            
                        [JsonProperty(LanguageEn ? "Plugin mute settings" : "Настройка мута в плагине")]
            public ControllerMute ControllerMutes = new ControllerMute();
            internal class ControllerMute
            {
                [JsonProperty(LanguageEn ? "Setting up automatic muting" : "Настройка автоматического мута")]
                public AutoMute AutoMuteSettings = new AutoMute();
                internal class AutoMute
                {
                    [JsonProperty(LanguageEn ? "Enable automatic muting for forbidden words (true - yes/false - no)" : "Включить автоматический мут по запрещенным словам(true - да/false - нет)")]
                    public Boolean UseAutoMute;
                    [JsonProperty(LanguageEn ? "Reason for automatic muting" : "Причина автоматического мута")]
                    public Muted AutoMuted;
                }
                [JsonProperty(LanguageEn ? "Additional setting for logging about mutes in discord" : "Дополнительная настройка для логирования о мутах в дискорд")]
                public LoggedFuncion LoggedMute = new LoggedFuncion();
                internal class LoggedFuncion
                {
                    [JsonProperty(LanguageEn ? "Support for logging the last N messages (Discord logging about mutes must be enabled)" : "Поддержка логирования последних N сообщений (Должно быть включено логирование в дискорд о мутах)")]
                    public Boolean UseHistoryMessage;
                    [JsonProperty(LanguageEn ? "How many latest player messages to send in logging" : "Сколько последних сообщений игрока отправлять в логировании")]
                    public Int32 CountHistoryMessage;
                }

                [JsonProperty(LanguageEn ? "Reasons to block chat" : "Причины для блокировки чата")]
                public List<Muted> MuteChatReasons = new List<Muted>();
                [JsonProperty(LanguageEn ? "Reasons to block your voice" : "Причины для блокировки голоса")]
                public List<Muted> MuteVoiceReasons = new List<Muted>();
                internal class Muted
                {
                    [JsonProperty(LanguageEn ? "Reason for blocking" : "Причина для блокировки")]
                    public String Reason;
                    [JsonProperty(LanguageEn ? "Block time (in seconds)" : "Время блокировки(в секундах)")]
                    public Int32 SecondMute;
                }
            }
            
                        [JsonProperty(LanguageEn ? "Configuring Message Processing" : "Настройка обработки сообщений")]
            public ControllerMessage ControllerMessages = new ControllerMessage();
            internal class ControllerMessage
            {
                [JsonProperty(LanguageEn ? "Basic settings for chat messages from the plugin" : "Основная настройка сообщений в чат от плагина")]
                public GeneralSettings GeneralSetting = new GeneralSettings();
                [JsonProperty(LanguageEn ? "Configuring functionality switching in chat" : "Настройка переключения функционала в чате")]
                public TurnedFuncional TurnedFunc = new TurnedFuncional();
                [JsonProperty(LanguageEn ? "Player message formatting settings" : "Настройка форматирования сообщений игроков")]
                public FormattingMessage Formatting = new FormattingMessage();
  
                
                internal class GeneralSettings
                {
                    [JsonProperty(LanguageEn ? "Customizing the chat alert format" : "Настройка формата оповещения в чате")]
                    public BroadcastSettings BroadcastFormat = new BroadcastSettings();
                    [JsonProperty(LanguageEn ? "Setting the mention format in the chat, via @" : "Настройка формата упоминания в чате, через @")]
                    public AlertSettings AlertFormat = new AlertSettings();
                    [JsonProperty(LanguageEn ? "Additional setting" : "Дополнительная настройка")]
                    public OtherSettings OtherSetting = new OtherSettings();
		   		 		  						  	   		  	  			  	  			  	 				  	  	
                    internal class BroadcastSettings
                    {
                        [JsonProperty(LanguageEn ? "The name of the notification in the chat" : "Наименование оповещения в чат")]
                        public String BroadcastTitle;
                        [JsonProperty(LanguageEn ? "Chat alert message color" : "Цвет сообщения оповещения в чат")]
                        public String BroadcastColor;
                        [JsonProperty(LanguageEn ? "Steam64ID for chat avatar" : "Steam64ID для аватарки в чате")]
                        public String Steam64IDAvatar;
                    }
                    internal class AlertSettings
                    {
                        [JsonProperty(LanguageEn ? "The color of the player mention message in the chat" : "Цвет сообщения упоминания игрока в чате")]
                        public String AlertPlayerColor;
                        [JsonProperty(LanguageEn ? "Sound when receiving and sending a mention via @" : "Звук при при получении и отправки упоминания через @")]
                        public String SoundAlertPlayer;
                    }
                    internal class OtherSettings
                    {
                        [JsonProperty(LanguageEn ? "Time after which the message will be deleted from the UI from the administrator" : "Время,через которое удалится сообщение с UI от администратора")]
                        public Int32 TimeDeleteAlertUI;

                        [JsonProperty(LanguageEn ? "The size of the message from the player in the chat" : "Размер сообщения от игрока в чате")]
                        public Int32 SizeMessage = 14;
                        [JsonProperty(LanguageEn ? "Player nickname size in chat" : "Размер ника игрока в чате")]
                        public Int32 SizeNick = 14;
                        [JsonProperty(LanguageEn ? "The size of the player's prefix in the chat (will be used if <size=N></size> is not set in the prefix itself)" : "Размер префикса игрока в чате (будет использовано, если в самом префиксе не установвлен <size=N></size>)")]
                        public Int32 SizePrefix = 14;
                    }
                }
                internal class TurnedFuncional
                {
                    [JsonProperty(LanguageEn ? "Configuring spam protection" : "Настройка защиты от спама")]
                    public AntiSpam AntiSpamSetting = new AntiSpam();
                    [JsonProperty(LanguageEn ? "Setting up a temporary chat block for newbies (who have just logged into the server)" : "Настройка временной блокировки чата новичкам (которые только зашли на сервер)")]
                    public AntiNoob AntiNoobSetting = new AntiNoob();
                    [JsonProperty(LanguageEn ? "Setting up private messages" : "Настройка личных сообщений")]
                    public PM PMSetting = new PM();

                    internal class AntiNoob
                    {
                        [JsonProperty(LanguageEn ? "Newbie protection in PM/R" : "Защита от новичка в PM/R")]
                        public Settings AntiNoobPM = new Settings();
                        [JsonProperty(LanguageEn ? "Newbie protection in global and team chat" : "Защита от новичка в глобальном и коммандном чате")]
                        public Settings AntiNoobChat = new Settings();
                        internal class Settings
                        {
                            [JsonProperty(LanguageEn ? "Enable protection?" : "Включить защиту?")]
                            public Boolean AntiNoobActivate = false;
                            [JsonProperty(LanguageEn ? "Newbie Chat Lock Time" : "Время блокировки чата для новичка")]
                            public Int32 TimeBlocked = 1200;
                        }
                    }
                    internal class AntiSpam
                    {
                        [JsonProperty(LanguageEn ? "Enable spam protection (Anti-spam)" : "Включить защиту от спама (Анти-спам)")]
                        public Boolean AntiSpamActivate;
                        [JsonProperty(LanguageEn ? "Time after which a player can send a message (AntiSpam)" : "Время через которое игрок может отправлять сообщение (АнтиСпам)")]
                        public Int32 FloodTime;
                        [JsonProperty(LanguageEn ? "Additional Anti-Spam settings" : "Дополнительная настройка Анти-Спама")]
                        public AntiSpamDuples AntiSpamDuplesSetting = new AntiSpamDuples();
                        internal class AntiSpamDuples
                        {
                            [JsonProperty(LanguageEn ? "Enable additional spam protection (Anti-duplicates, duplicate messages)" : "Включить дополнительную защиту от спама (Анти-дубликаты, повторяющие сообщения)")]
                            public Boolean AntiSpamDuplesActivate = true;
                            [JsonProperty(LanguageEn ? "How many duplicate messages does a player need to make to be confused by the system" : "Сколько дублирующих сообщений нужно сделать игроку чтобы его замутила система")]
                            public Int32 TryDuples = 3;
                            [JsonProperty(LanguageEn ? "Setting up automatic muting for duplicates" : "Настройка автоматического мута за дубликаты")]
                            public ControllerMute.Muted MuteSetting = new ControllerMute.Muted
                            {
                                Reason = LanguageEn ? "Blocking for duplicate messages (SPAM)" : "Блокировка за дублирующие сообщения (СПАМ)",
                                SecondMute = 300,
                            };
                        }
                    }
                    internal class PM
                    {
                        [JsonProperty(LanguageEn ? "Enable Private Messages" : "Включить личные сообщения")]
                        public Boolean PMActivate;
                        [JsonProperty(LanguageEn ? "Sound when receiving a private message" : "Звук при при получении личного сообщения")]
                        public String SoundPM;
                    }
                    [JsonProperty(LanguageEn ? "Enable PM ignore for players (/ignore nick or via interface)" : "Включить игнор ЛС игрокам(/ignore nick или через интерфейс)")]
                    public Boolean IgnoreUsePM;
                    [JsonProperty(LanguageEn ? "Hide the issue of items to the Admin from the chat" : "Скрыть из чата выдачу предметов Админу")]
                    public Boolean HideAdminGave;
                    [JsonProperty(LanguageEn ? "Move mute to team chat (In case of a mute, the player will not be able to write even to the team chat)" : "Переносить мут в командный чат(В случае мута, игрок не сможет писать даже в командный чат)")]
                    public Boolean MuteTeamChat;
                }
                internal class FormattingMessage
                {
                    [JsonProperty(LanguageEn ? "Enable message formatting [Will control caps, message format] (true - yes/false - no)" : "Включить форматирование сообщений [Будет контроллировать капс, формат сообщения] (true - да/false - нет)")]
                    public Boolean FormatMessage;
                    [JsonProperty(LanguageEn ? "Use a list of banned words (true - yes/false - no)" : "Использовать список запрещенных слов (true - да/false - нет)")]
                    public Boolean UseBadWords;
                    [JsonProperty(LanguageEn ? "The word that will replace the forbidden word" : "Слово которое будет заменять запрещенное слово")]
                    public String ReplaceBadWord;
                    [JsonProperty(LanguageEn ? "List of banned words" : "Список запрещенных слов")]
                    public List<String> BadWords = new List<String>();

                    [JsonProperty(LanguageEn ? "Nickname controller setup" : "Настройка контроллера ников")]
                    public NickController ControllerNickname = new NickController();
                    internal class NickController
                    {
                        [JsonProperty(LanguageEn ? "Enable player nickname formatting (message formatting must be enabled)" : "Включить форматирование ников игроков (должно быть включено форматирование сообщений)")]
                        public Boolean UseNickController = true;
                        [JsonProperty(LanguageEn ? "The word that will replace the forbidden word (You can leave it blank and it will just delete)" : "Слово которое будет заменять запрещенное слово (Вы можете оставить пустым и будет просто удалять)")]
                        public String ReplaceBadNick = "****";
                        [JsonProperty(LanguageEn ? "List of banned nicknames" : "Список запрещенных ников")]
                        public List<String> BadNicks = new List<String>();
                        [JsonProperty(LanguageEn ? "List of allowed links in nicknames" : "Список разрешенных ссылок в никах")]
                        public List<String> AllowedLinkNick = new List<String>();
                    }
                }
            }

            
            
            [JsonProperty(LanguageEn ? "Setting up chat alerts" : "Настройка оповещений в чате")]
            public ControllerAlert ControllerAlertSetting;

            internal class ControllerAlert
            {
                [JsonProperty(LanguageEn ? "Setting up chat alerts" : "Настройка оповещений в чате")]
                public Alert AlertSetting;
                [JsonProperty(LanguageEn ? "Setting notifications about the status of the player's session" : "Настройка оповещений о статусе сессии игрока")]
                public PlayerSession PlayerSessionSetting;
                [JsonProperty(LanguageEn ? "Configuring administrator session status alerts" : "Настройка оповещений о статусе сессии администратора")]
                public AdminSession AdminSessionSetting;
                [JsonProperty(LanguageEn ? "Setting up personal notifications to the player when connecting" : "Настройка персональных оповоещений игроку при коннекте")]
                public PersonalAlert PersonalAlertSetting;
                internal class Alert
                {
                    [JsonProperty(LanguageEn ? "Enable automatic messages in chat (true - yes/false - no)" : "Включить автоматические сообщения в чат (true - да/false - нет)")]
                    public Boolean AlertMessage;
                    [JsonProperty(LanguageEn ? "Type of automatic messages : true - sequential / false - random" : "Тип автоматических сообщений : true - поочередные/false - случайные")]
                    public Boolean AlertMessageType;

                    [JsonProperty(LanguageEn ? "List of automatic messages in chat" : "Список автоматических сообщений в чат")]
                    public LanguageController MessageList = new LanguageController();
                    [JsonProperty(LanguageEn ? "Interval for sending messages to chat (Broadcaster) (in seconds)" : "Интервал отправки сообщений в чат (Броадкастер) (в секундах)")]
                    public Int32 MessageListTimer;
                }
                internal class PlayerSession
                {
                    [JsonProperty(LanguageEn ? "When a player is notified about the entry / exit of the player, display his avatar opposite the nickname (true - yes / false - no)" : "При уведомлении о входе/выходе игрока отображать его аватар напротив ника (true - да/false - нет)")]
                    public Boolean ConnectedAvatarUse;

                    [JsonProperty(LanguageEn ? "Notify in chat when a player enters (true - yes/false - no)" : "Уведомлять в чате о входе игрока (true - да/false - нет)")]
                    public Boolean ConnectedAlert;
                    [JsonProperty(LanguageEn ? "Enable random notifications when a player from the list enters (true - yes / false - no)" : "Включить случайные уведомления о входе игрока из списка (true - да/false - нет)")]
                    public Boolean ConnectionAlertRandom;
                    [JsonProperty(LanguageEn ? "Show the country of the entered player (true - yes/false - no)" : "Отображать страну зашедшего игрока (true - да/false - нет")]
                    public Boolean ConnectedWorld;

                    [JsonProperty(LanguageEn ? "Notify when a player enters the chat (selected from the list) (true - yes/false - no)" : "Уведомлять о выходе игрока в чат(выбираются из списка) (true - да/false - нет)")]
                    public Boolean DisconnectedAlert;
                    [JsonProperty(LanguageEn ? "Enable random player exit notifications (true - yes/false - no)" : "Включить случайные уведомления о выходе игрока (true - да/false - нет)")]
                    public Boolean DisconnectedAlertRandom;
                    [JsonProperty(LanguageEn ? "Display reason for player exit (true - yes/false - no)" : "Отображать причину выхода игрока (true - да/false - нет)")]
                    public Boolean DisconnectedReason;

                    [JsonProperty(LanguageEn ? "Random player entry notifications({0} - player's nickname, {1} - country (if country display is enabled)" : "Случайные уведомления о входе игрока({0} - ник игрока, {1} - страна(если включено отображение страны)")]
                    public LanguageController RandomConnectionAlert = new LanguageController();
                    [JsonProperty(LanguageEn ? "Random notifications about the exit of the player ({0} - player's nickname, {1} - the reason for the exit (if the reason is enabled)" : "Случайные уведомления о выходе игрока({0} - ник игрока, {1} - причина выхода(если включена причина)")]
                    public LanguageController RandomDisconnectedAlert = new LanguageController();
                }
                internal class AdminSession
                {
                    [JsonProperty(LanguageEn ? "Notify admin on the server in the chat (true - yes/false - no)" : "Уведомлять о входе админа на сервер в чат (true - да/false - нет)")]
                    public Boolean ConnectedAlertAdmin;
                    [JsonProperty(LanguageEn ? "Notify about admin leaving the server in chat (true - yes/false - no)" : "Уведомлять о выходе админа на сервер в чат (true - да/false - нет)")]
                    public Boolean DisconnectedAlertAdmin;
                }
                internal class PersonalAlert
                {
                    [JsonProperty(LanguageEn ? "Enable random message to the player who has logged in (true - yes/false - no)" : "Включить случайное сообщение зашедшему игроку (true - да/false - нет)")]
                    public Boolean UseWelcomeMessage;
                    [JsonProperty(LanguageEn ? "List of messages to the player when entering" : "Список сообщений игроку при входе")]
                    public LanguageController WelcomeMessage = new LanguageController();
                }
            }

            public class LanguageController
            {
                [JsonProperty(LanguageEn ? "Setting up Multilingual Messages [Language Code] = Translation Variations" : "Настройка мультиязычных сообщений [КодЯзыка] = ВариацииПеревода")]
                public Dictionary<String, List<String>> LanguageMessages = new Dictionary<String, List<String>>();
            }

            
                        [JsonProperty(LanguageEn ? "Settings Rust+" : "Настройка Rust+")]
            public RustPlus RustPlusSettings;
            internal class RustPlus
            {
                [JsonProperty(LanguageEn ? "Use Rust+" : "Использовать Rust+")]
                public Boolean UseRustPlus;
                [JsonProperty(LanguageEn ? "Title for notification Rust+" : "Название для уведомления Rust+")]
                public String DisplayNameAlert;
            }
            
                        [JsonProperty(LanguageEn ? "Configuring support plugins" : "Настройка плагинов поддержки")]
            public ReferenceSettings ReferenceSetting = new ReferenceSettings();
            internal class ReferenceSettings
            {
                [JsonProperty(LanguageEn ? "Settings XLevels" : "Настройка XLevels")]
                public XLevels XLevelsSettings = new XLevels();
                [JsonProperty(LanguageEn ? "Settings IQFakeActive" : "Настройка IQFakeActive")]
                public IQFakeActive IQFakeActiveSettings = new IQFakeActive();
                [JsonProperty(LanguageEn ? "Settings IQRankSystem" : "Настройка IQRankSystem")]
                public IQRankSystem IQRankSystems = new IQRankSystem();
                [JsonProperty(LanguageEn ? "Settings Clans" : "Настройка Clans")]
                public Clans ClansSettings = new Clans();

                internal class Clans
                {
                    [JsonProperty(LanguageEn ? "Display a clan tag in the chat (if Clans are installed)" : "Отображать в чате клановый тэг (если установлены Clans)")]
                    public Boolean UseClanTag;
                }
                internal class IQRankSystem
                {
                    [JsonProperty(LanguageEn ? "Rank display format in chat ( {0} is the user's rank, do not delete this value)" : "Формат отображения ранга в чате ( {0} - это ранг юзера, не удаляйте это значение)")]
                    public String FormatRank = "[{0}]";
                    [JsonProperty(LanguageEn ? "Time display format with IQRank System in chat ( {0} is the user's time, do not delete this value)" : "Формат отображения времени с IQRankSystem в чате ( {0} - это время юзера, не удаляйте это значение)")]
                    public String FormatRankTime = "[{0}]";
                    [JsonProperty(LanguageEn ? "Use support IQRankSystem" : "Использовать поддержку рангов")]
                    public Boolean UseRankSystem;
                    [JsonProperty(LanguageEn ? "Show players their played time next to their rank" : "Отображать игрокам их отыгранное время рядом с рангом")]
                    public Boolean UseTimeStandart;
                }
                internal class IQFakeActive
                {
                    [JsonProperty(LanguageEn ? "Use support IQFakeActive" : "Использовать поддержку IQFakeActive")]
                    public Boolean UseIQFakeActive;
                }
                internal class XLevels
                {
                    [JsonProperty(LanguageEn ? "Use support XLevels" : "Использовать поддержку XLevels")]
                    public Boolean UseXLevels;
                    [JsonProperty(LanguageEn ? "Use full prefix with level from XLevel (true) otherwise only level (false)" : "Использовать полный префикс с уровнем из XLevel (true) иначе только уровень (false)")]
                    public Boolean UseFullXLevels;
                }
            }
            
            
            [JsonProperty(LanguageEn ? "Setting up an answering machine" : "Настройка автоответчика")]
            public AnswerMessage AnswerMessages = new AnswerMessage();

            internal class AnswerMessage
            {
                [JsonProperty(LanguageEn ? "Enable auto-reply? (true - yes/false - no)" : "Включить автоответчик?(true - да/false - нет)")]
                public bool UseAnswer;
                [JsonProperty(LanguageEn ? "Customize Messages [Keyword] = Reply" : "Настройка сообщений [Ключевое слово] = Ответ")]
                public Dictionary<String, LanguageController> AnswerMessageList = new Dictionary<String, LanguageController>();
            }

            
                        [JsonProperty(LanguageEn ? "Additional setting" : "Дополнительная настройка")]
            public OtherSettings OtherSetting;

            internal class OtherSettings
            {
                [JsonProperty(LanguageEn ? "Enable the /online command (true - yes / false - no)" : "Включить команду /online (true - да/ false - нет)")]
                public Boolean UseCommandOnline;
                [JsonProperty(LanguageEn ? "Setting up message logging" : "Настройка логирования сообщений")]
                public LoggedChat LogsChat = new LoggedChat();
                [JsonProperty(LanguageEn ? "Setting up logging of personal messages of players" : "Настройка логирования личных сообщений игроков")]
                public General LogsPMChat = new General();
                [JsonProperty(LanguageEn ? "Setting up chat/voice lock/unlock logging" : "Настройка логирования блокировок/разблокировок чата/голоса")]
                public General LogsMuted = new General();
                [JsonProperty(LanguageEn ? "Setting up logging of chat commands from players" : "Настройка логирования чат-команд от игроков")]
                public General LogsChatCommands = new General();
                internal class LoggedChat
                {
                    [JsonProperty(LanguageEn ? "Setting up general chat logging" : "Настройка логирования общего чата")]
                    public General GlobalChatSettings = new General();
                    [JsonProperty(LanguageEn ? "Setting up team chat logging" : "Настройка логирования тим чата")]
                    public General TeamChatSettings = new General();
                }
                internal class General
                {
                    [JsonProperty(LanguageEn ? "Enable logging (true - yes/false - no)" : "Включить логирование (true - да/false - нет)")]
                    public Boolean UseLogged = false;
                    [JsonProperty(LanguageEn ? "Webhooks channel for logging" : "Webhooks канала для логирования")]
                    public String Webhooks = "";
                }
            }
            
            public static Configuration GetNewConfiguration()
            {
                return new Configuration
                {
                                        ControllerParameter = new ControllerParameters
                    {
                        VisualParametres = new ControllerParameters.VisualSettingParametres
                        {
                            PrefixType = SelectedParametres.DropList,
                            ChatColorType = SelectedParametres.DropList,
                            NickColorType = SelectedParametres.Slider,
                            IQRankSystemType = SelectedParametres.Slider,
                        },
                        Prefixes = new ControllerParameters.PrefixSetting
                        {
                            TurnMultiPrefixes = false,
                            MaximumMultiPrefixCount = 5,
                            Prefixes = new List<ControllerParameters.AdvancedFuncion>
                              {
                                  new ControllerParameters.AdvancedFuncion
                                  {
                                      Argument = LanguageEn ? "<color=#CC99FF>[PLAYER]</color>" : "<color=#CC99FF>[ИГРОК]</color>",
                                      Permissions = "iqchat.default",
                                      IsBlockSelected = false,
                                  },
                                  new ControllerParameters.AdvancedFuncion
                                  {
                                      Argument = "<color=#ffff99>[VIP]</color>",
                                      Permissions = "iqchat.admin",
                                      IsBlockSelected = false,
                                  },
                                  new ControllerParameters.AdvancedFuncion
                                  {
                                      Argument = LanguageEn ? "<color=#ff9999>[ADMIN]</color>" : "<color=#ff9999>[АДМИН]</color>",
                                      Permissions = "iqchat.admin",
                                      IsBlockSelected = false,
                                  },
                            },
                        },
                        MessageColorList = new List<ControllerParameters.AdvancedFuncion>
                        {
                               new ControllerParameters.AdvancedFuncion
                               {
                                    Argument = "#CC99FF",
                                    Permissions = "iqchat.default",
                                    IsBlockSelected = false,
                               },
                               new ControllerParameters.AdvancedFuncion
                               {
                                    Argument = "#ffff99",
                                    Permissions = "iqchat.admin",
                                    IsBlockSelected = false,
                               },
                               new ControllerParameters.AdvancedFuncion
                               {
                                    Argument = "#ff9999",
                                    Permissions = "iqchat.admin",
                                    IsBlockSelected = false,
                               },
                        },
                        NickColorList = new List<ControllerParameters.AdvancedFuncion>
                        {
                               new ControllerParameters.AdvancedFuncion
                               {
                                    Argument = "#CC99FF",
                                    Permissions = "iqchat.default",
                                    IsBlockSelected = false,
                               },
                               new ControllerParameters.AdvancedFuncion
                               {
                                    Argument = "#ffff99",
                                    Permissions = "iqchat.admin",
                                    IsBlockSelected = false,
                               },
                               new ControllerParameters.AdvancedFuncion
                               {
                                    Argument = "#ff9999",
                                    Permissions = "iqchat.admin",
                                    IsBlockSelected = false,
                               },
                        },
                    },
                    
                    
                    ControllerConnect = new ControllerConnection
                    {
                        SetupDefaults = new ControllerConnection.SetupDefault
                        {
                            PrefixDefault = LanguageEn ? "<color=#CC99FF>[PLAYER]</color>" : "<color=#CC99FF>[ИГРОК]</color>",
                            MessageDefault = "#33CCCC",
                            NickDefault = "#0099FF",
                        },
                        Turneds = new ControllerConnection.Turned
                        {
                            TurnAutoDropColorChat = true,
                            TurnAutoDropColorNick = true,
                            TurnAutoDropPrefix = true,
                            TurnAutoSetupColorChat = true,
                            TurnAutoSetupColorNick = true,
                            TurnAutoSetupPrefix = true,
                        }
                    },

                    
                    
                    ControllerMutes = new ControllerMute
                    {
                        LoggedMute = new ControllerMute.LoggedFuncion
                        {
                            UseHistoryMessage = false,
                            CountHistoryMessage = 10,
                        },
                        AutoMuteSettings = new ControllerMute.AutoMute
                        {
                            UseAutoMute = true,
                            AutoMuted = new ControllerMute.Muted
                            {
                                Reason = LanguageEn ? "Automatic chat blocking" : "Автоматическая блокировка чата",
                                SecondMute = 300,
                            }
                        },
                        MuteChatReasons = new List<ControllerMute.Muted>
                        {
                            new ControllerMute.Muted
                            {
                                Reason = LanguageEn ? "Aggressive behavior" : "Агрессивное поведение",
                                SecondMute = 100,
                            },
                            new ControllerMute.Muted
                            {
                                Reason = LanguageEn ? "Insults" : "Оскорбления",
                                SecondMute = 300,
                            },
                            new ControllerMute.Muted
                            {
                                Reason = LanguageEn ? "Insult (repeated violation)" : "Оскорбление (повторное нарушение)",
                                SecondMute = 1000,
                            },
                            new ControllerMute.Muted
                            {
                                Reason = LanguageEn ? "Advertising" : "Реклама",
                                SecondMute = 5000,
                            },
                            new ControllerMute.Muted
                            {
                                Reason = LanguageEn ? "Humiliation" : "Унижение",
                                SecondMute = 300,
                            },
                            new ControllerMute.Muted
                            {
                                Reason = LanguageEn ? "Spam" : "Спам",
                                SecondMute = 60,
                            },
                        },
                        MuteVoiceReasons = new List<ControllerMute.Muted>
                        {
                            new ControllerMute.Muted
                            {
                                Reason = LanguageEn ? "Aggressive behavior" : "Агрессивное поведение",
                                SecondMute = 100,
                            },
                            new ControllerMute.Muted
                            {
                                Reason = LanguageEn ? "Insults" : "Оскорбления",
                                SecondMute = 300,
                            },
                            new ControllerMute.Muted
                            {
                                Reason = LanguageEn ? "Disruption of the event by shouting" : "Срыв мероприятия криками",
                                SecondMute = 300,
                            },
                        }
                    },

                    
                    
                    ControllerMessages = new ControllerMessage
                    {
                        Formatting = new ControllerMessage.FormattingMessage
                        {
                            UseBadWords = true,
                            BadWords = LanguageEn ? new List<String> { "fuckyou", "sucking", "fucking", "fuck" } : new List<String> { "бля", "сука", "говно", "тварь" },
                            FormatMessage = true,
                            ReplaceBadWord = "***",
                            ControllerNickname = new ControllerMessage.FormattingMessage.NickController
                            {
                                BadNicks = LanguageEn ? new List<String> { "Admin", "Moderator", "Administrator", "Moder", "Owner", "Mercury Loh", "IQchat" } : new List<String> { "Администратор", "Модератор", "Админ", "Модер", "Овнер", "Mercury Loh", "IQchat" },
                                AllowedLinkNick = new List<String> { "mysite.com" },
                                ReplaceBadNick = "",
                                UseNickController = true,
                            },
                        },
                        TurnedFunc = new ControllerMessage.TurnedFuncional
                        {
                            HideAdminGave = true,
                            IgnoreUsePM = true,
                            MuteTeamChat = true,
                            AntiNoobSetting = new ControllerMessage.TurnedFuncional.AntiNoob
                            {
                                AntiNoobChat = new ControllerMessage.TurnedFuncional.AntiNoob.Settings
                                {
                                    AntiNoobActivate = false,
                                    TimeBlocked = 1200,
                                },
                                AntiNoobPM = new ControllerMessage.TurnedFuncional.AntiNoob.Settings
                                {
                                    AntiNoobActivate = false,
                                    TimeBlocked = 1200,
                                },
                            },
                            AntiSpamSetting = new ControllerMessage.TurnedFuncional.AntiSpam
                            {
                                AntiSpamActivate = true,
                                FloodTime = 10,
                                AntiSpamDuplesSetting = new ControllerMessage.TurnedFuncional.AntiSpam.AntiSpamDuples
                                {
                                    AntiSpamDuplesActivate = true,
                                    MuteSetting = new ControllerMute.Muted
                                    {
                                        Reason = LanguageEn ? "Duplicate messages (SPAM)" : "Повторяющиеся сообщения (СПАМ)",
                                        SecondMute = 300,
                                    },
                                    TryDuples = 3,
                                }
                            },
                            PMSetting = new ControllerMessage.TurnedFuncional.PM
                            {
                                PMActivate = true,
                                SoundPM = "assets/bundled/prefabs/fx/notice/stack.world.fx.prefab",
                            },
                        },
                        GeneralSetting = new ControllerMessage.GeneralSettings
                        {
                            BroadcastFormat = new ControllerMessage.GeneralSettings.BroadcastSettings
                            {
                                BroadcastColor = "#efedee",
                                BroadcastTitle = LanguageEn ? "<color=#68cacd><b>[Alert]</b></color>" : "<color=#68cacd><b>[ОПОВЕЩЕНИЕ]</b></color>",
                                Steam64IDAvatar = "0",
                            },
                            AlertFormat = new ControllerMessage.GeneralSettings.AlertSettings
                            {
                                AlertPlayerColor = "#efedee",
                                SoundAlertPlayer = "assets/bundled/prefabs/fx/notice/item.select.fx.prefab",
                            },
                            OtherSetting = new ControllerMessage.GeneralSettings.OtherSettings
                            {
                                TimeDeleteAlertUI = 5,
                                SizePrefix = 14,
                                SizeMessage = 14,
                                SizeNick = 14,
                            }
                        },
                    },

                    
                    
                    ControllerAlertSetting = new ControllerAlert
                    {
                        AlertSetting = new ControllerAlert.Alert
                        {
                            AlertMessage = true,
                            AlertMessageType = false,
                            MessageList = new LanguageController()
                            {
                                LanguageMessages = new Dictionary<String, List<String>>()
                                {
                                    ["en"] = new List<String>()
                                    {
                                        "Automatic message #1 (Edit in configuration)",
                                        "Automatic message #2 (Edit in configuration)",
                                        "Automatic message #3 (Edit in configuration)",
                                        "Automatic message #4 (Edit in configuration)",
                                        "Automatic message #5 (Edit in configuration)",
                                        "Automatic message #6 (Edit in configuration)",
                                    },
                                    ["ru"] = new List<String>()
                                    {
                                        "Автоматическое сообщение #1 (Редактировать в конфигурации)",
                                        "Автоматическое сообщение #2 (Редактировать в конфигурации)",
                                        "Автоматическое сообщение #3 (Редактировать в конфигурации)",
                                        "Автоматическое сообщение #4 (Редактировать в конфигурации)",
                                        "Автоматическое сообщение #5 (Редактировать в конфигурации)",
                                        "Автоматическое сообщение #6 (Редактировать в конфигурации)",
                                    }
                                },
                            },
                            MessageListTimer = 60,
                        },
                        AdminSessionSetting = new ControllerAlert.AdminSession
                        {
                            ConnectedAlertAdmin = false,
                            DisconnectedAlertAdmin = false,
                        },
                        PlayerSessionSetting = new ControllerAlert.PlayerSession
                        {
                            ConnectedAlert = true,
                            ConnectedAvatarUse = true,
                            ConnectedWorld = true,
                            ConnectionAlertRandom = false,

                            DisconnectedAlert = true,
                            DisconnectedAlertRandom = false,
                            DisconnectedReason = true,

                            RandomConnectionAlert = new LanguageController
                            {
                                LanguageMessages = new Dictionary<String, List<String>>()
                                {
                                    ["en"] = new List<String>()
                                    {
                                        "{0} flew in from {1}",
                                        "{0} flew into the server from{1}",
                                        "{0} jumped on a server"
                                    },
                                    ["ru"] = new List<String>()
                                    {
                                        "{0} влетел как дурачок из {1}",
                                        "{0} залетел на сервер из {1}, соболезнуем",
                                        "{0} прыгнул на сервачок"
                                    }
                                }
                            },
                            RandomDisconnectedAlert = new LanguageController()
                            {
                                LanguageMessages = new Dictionary<String, List<String>>()
                                {
                                    ["en"] = new List<String>()
                                    {
                                        "{0} gone to another world",
                                        "{0} left the server with a reason {1}",
                                        "{0} went to another server"
                                    },
                                    ["ru"] = new List<String>()
                                    {
                                        "{0} ушел в мир иной",
                                        "{0} вылетел с сервера с причиной {1}",
                                        "{0} пошел на другой сервачок"
                                    }
                                }
                            },
                        },
                        PersonalAlertSetting = new ControllerAlert.PersonalAlert
                        {
                            UseWelcomeMessage = true,
                            WelcomeMessage = new LanguageController
                            {
                                LanguageMessages = new Dictionary<String, List<String>>()
                                {
                                    ["en"] = new List<String>()
                                    {
                                        "Welcome to the server SUPERSERVER\nWe are glad that you chose us!",
                                        "Welcome back to the server!\nWe wish you good luck",
                                        "Welcome to the server\nWe have the best plugins",
                                    },
                                    ["ru"] = new List<String>()
                                    {
                                        "Добро пожаловать на сервер SUPERSERVER\nРады,что выбрал именно нас!",
                                        "С возвращением на сервер!\nЖелаем тебе удачи",
                                        "Добро пожаловать на сервер\nУ нас самые лучшие плагины",
                                    }
                                }
                            },
                        }
                    },

                    
                    
                    ReferenceSetting = new ReferenceSettings
                    {
                        IQFakeActiveSettings = new ReferenceSettings.IQFakeActive
                        {
                            UseIQFakeActive = true,
                        },
                        IQRankSystems = new ReferenceSettings.IQRankSystem
                        {
                            FormatRank = "[{0}]",
                            FormatRankTime = "[{0}]",
                            UseRankSystem = false,
                            UseTimeStandart = true
                        },
                        XLevelsSettings = new ReferenceSettings.XLevels()
                        {
                            UseXLevels = false,
                            UseFullXLevels = false,
                        },
                        ClansSettings = new ReferenceSettings.Clans()
                        {
                            UseClanTag = false,
                        }
                    },

                    
                    
                    RustPlusSettings = new RustPlus
                    {
                        UseRustPlus = true,
                        DisplayNameAlert = LanguageEn ? "SUPER SERVER" : "СУПЕР СЕРВЕР",
                    },

                    
                    
                    AnswerMessages = new AnswerMessage
                    {
                        UseAnswer = true,
                        AnswerMessageList = new Dictionary<String, LanguageController>()
                        {
                            ["wipe"] = new LanguageController()
                            {
                                LanguageMessages = new Dictionary<String, List<String>>()
                                {
                                    ["en"] = new List<String>()
                                    {
                                        "Wipe will be 27.06"
                                    },
                                    ["ru"] = new List<String>()
                                    {
                                        "Вайп будет 27.06"
                                    }
                                }
                            },
                            ["читер"] = new LanguageController()
                            {
                                LanguageMessages = new Dictionary<String, List<String>>()
                                {
                                    ["en"] = new List<String>()
                                    {
                                        "Found a cheater? Write /report and send a complaint"
                                    },
                                    ["ru"] = new List<String>()
                                    {
                                        "Нашли читера?Напиши /report и отправь жалобу"
                                    }
                                }
                            }
                        },
                    },

                    
                    
                    OtherSetting = new OtherSettings
                    {
                        UseCommandOnline = false,
                        LogsChat = new OtherSettings.LoggedChat
                        {
                            GlobalChatSettings = new OtherSettings.General
                            {
                                UseLogged = false,
                                Webhooks = "",
                            },
                            TeamChatSettings = new OtherSettings.General
                            {
                                UseLogged = false,
                                Webhooks = "",
                            }
                        },
                        LogsChatCommands = new OtherSettings.General
                        {
                            UseLogged = false,
                            Webhooks = "",
                        },
                        LogsPMChat = new OtherSettings.General
                        {
                            UseLogged = false,
                            Webhooks = "",
                        },
                        LogsMuted = new OtherSettings.General
                        {
                            UseLogged = false,
                            Webhooks = "",
                        },
                    },

                                    };
            }
        }
        private const String PermissionAntiSpam = "iqchat.antispamabuse";
        public List<FakePlayer> PlayerBases = new List<FakePlayer>();

        private void AlertDisconnected(BasePlayer player, String reason)
        {
            Configuration.ControllerAlert.AdminSession AlertSessionAdmin = config.ControllerAlertSetting.AdminSessionSetting;
            Configuration.ControllerAlert.PlayerSession AlertSessionPlayer = config.ControllerAlertSetting.PlayerSessionSetting;
            GeneralInformation.RenameInfo RenameInformation = GeneralInfo.GetInfoRename(player.userID);

            if (AlertSessionPlayer.DisconnectedAlert)
            {
                if (!AlertSessionAdmin.DisconnectedAlertAdmin)
                    if (player.IsAdmin) return;

                String DisplayName = player.displayName;

                // Configuration.ControllerMessage ControllerMessage = config.ControllerMessages;

                // if (ControllerMessage.Formatting.ControllerNickname.UseNickController)
                //     foreach (String DetectedBadNick in DisplayName.Split(' '))
                //     {
                //         if (ControllerMessage.Formatting.ControllerNickname.BadNicks.Count(x => x.ToLower() == DetectedBadNick.ToLower()) > 0 && DetectedBadNick.Leght != 44423251)
                //             DisplayName = DisplayName.Replace(DetectedBadNick, ControllerMessage.Formatting.ControllerNickname.ReplaceBadNick);
                //     }

                UInt64 UserID = player.userID;
                if (RenameInformation != null)
                {
                    DisplayName = RenameInformation.RenameNick;
                    UserID = RenameInformation.RenameID;
                }

                String Avatar = AlertSessionPlayer.ConnectedAvatarUse ? UserID.ToString() : String.Empty;

                if (!permission.UserHasPermission(player.UserIDString, PermissionHideDisconnection))
                {
                    if (AlertSessionPlayer.DisconnectedAlertRandom)
                        ReplyBroadcast(null, Avatar, false, AlertSessionPlayer.RandomDisconnectedAlert.LanguageMessages,DisplayName, reason);
                    else
                    {
                        System.Object[] args = AlertSessionPlayer.DisconnectedReason ? new System.Object[] { DisplayName, reason } : new System.Object[] { DisplayName };
                        String Lang = AlertSessionPlayer.DisconnectedReason ? "LEAVE_PLAYER_REASON" : "LEAVE_PLAYER";
                        ReplyBroadcast(null, Avatar, false, Lang, args);
                    }
                }

                Log($"[{player.userID}] {(AlertSessionPlayer.DisconnectedReason ? GetLang("LEAVE_PLAYER_REASON", player.UserIDString, DisplayName, reason) : GetLang("LEAVE_PLAYER", player.UserIDString, DisplayName))}");
            }
        }
        
                private void DrawUI_IQChat_Mute_Alert(BasePlayer player, BasePlayer Target, UInt64 IDFake = 0)
        {
            String InterfacePanel = InterfaceBuilder.GetInterface("UI_Chat_Mute_And_Ignore_Alert_Panel");
            String Interface = InterfaceBuilder.GetInterface("UI_Chat_Mute_Alert");
            if (Interface == null || InterfacePanel == null) return;

            User InfoTarget = (IQFakeActive && Target == null && IDFake != 0) ? null : UserInformation[Target.userID];

            Interface = Interface.Replace("%TITLE%", GetLang("IQCHAT_BUTTON_MODERATION_MUTE_MENU_TITLE_ALERT", player.UserIDString));
            Interface = Interface.Replace("%BUTTON_TAKE_CHAT_ACTION%", InfoTarget == null ? GetLang("IQCHAT_BUTTON_MODERATION_MUTE_MENU_TITLE_ALERT_CHAT", player.UserIDString) : InfoTarget.MuteInfo.IsMute(MuteType.Chat) ? GetLang("IQCHAT_BUTTON_MODERATION_UNMUTE_MENU_TITLE_ALERT_CHAT", player.UserIDString) : GetLang("IQCHAT_BUTTON_MODERATION_MUTE_MENU_TITLE_ALERT_CHAT", player.UserIDString));
            Interface = Interface.Replace("%BUTTON_TAKE_VOICE_ACTION%", InfoTarget == null ? GetLang("IQCHAT_BUTTON_MODERATION_MUTE_MENU_TITLE_ALERT_VOICE", player.UserIDString) : InfoTarget.MuteInfo.IsMute(MuteType.Voice) ? GetLang("IQCHAT_BUTTON_MODERATION_UNMUTE_MENU_TITLE_ALERT_VOICE", player.UserIDString) : GetLang("IQCHAT_BUTTON_MODERATION_MUTE_MENU_TITLE_ALERT_VOICE", player.UserIDString));
            Interface = Interface.Replace("%COMMAND_TAKE_ACTION_MUTE_CHAT%", InfoTarget == null ? $"newui.cmd action.mute.ignore ignore.and.mute.controller {SelectedAction.Mute} open.reason.mute {IDFake} {MuteType.Chat}" : InfoTarget.MuteInfo.IsMute(MuteType.Chat) ? $"newui.cmd action.mute.ignore ignore.and.mute.controller {SelectedAction.Mute} unmute.yes {Target.UserIDString} {MuteType.Chat}" : $"newui.cmd action.mute.ignore ignore.and.mute.controller {SelectedAction.Mute} open.reason.mute {Target.UserIDString} {MuteType.Chat}");
            Interface = Interface.Replace("%COMMAND_TAKE_ACTION_MUTE_VOICE%", InfoTarget == null ? $"newui.cmd action.mute.ignore ignore.and.mute.controller {SelectedAction.Mute} open.reason.mute {IDFake} {MuteType.Voice}" : InfoTarget.MuteInfo.IsMute(MuteType.Voice) ? $"newui.cmd action.mute.ignore ignore.and.mute.controller {SelectedAction.Mute} unmute.yes {Target.UserIDString} {MuteType.Voice}" : $"newui.cmd action.mute.ignore ignore.and.mute.controller {SelectedAction.Mute} open.reason.mute {Target.UserIDString} {MuteType.Voice}");

            CuiHelper.DestroyUi(player, "MUTE_AND_IGNORE_PANEL_ALERT");
            CuiHelper.AddUi(player, InterfacePanel);
            CuiHelper.AddUi(player, Interface);
        }
        void SyncReservedFinish(string JSON)
        {
            if (!config.ReferenceSetting.IQFakeActiveSettings.UseIQFakeActive) return;
            List<FakePlayer> ContentDeserialize = JsonConvert.DeserializeObject<List<FakePlayer>>(JSON);
            PlayerBases = ContentDeserialize;

            PrintWarning(LanguageEn ? "IQChat - successfully synced with IQFakeActive" : "IQChat - успешно синхронизирована с IQFakeActive");
            PrintWarning("=============SYNC==================");
        }
        
                private void MutePlayer(BasePlayer Target, MuteType Type, Int32 ReasonIndex, BasePlayer Moderator = null, String ReasonCustom = null, Int32 TimeCustom = 0, Boolean HideMute = false, Boolean Command = false, UInt64 IDFake = 0)
        {
            Configuration.ControllerMute ControllerMutes = config.ControllerMutes;

            if (IQFakeActive && Target == null && (IQFakeActive && Target == null && IDFake != 0))
            {
                ReplySystem(Moderator, GetLang(Type == MuteType.Chat ? "FUNC_MESSAGE_MUTE_CHAT" : "FUNC_MESSAGE_MUTE_VOICE", Moderator != null ? Moderator.displayName : Moderator.UserIDString, GetLang("IQCHAT_FUNCED_ALERT_TITLE_SERVER"), FindFakeName(IDFake), FormatTime(TimeCustom == 0 ? config.ControllerMutes.MuteChatReasons[ReasonIndex].SecondMute : TimeCustom), ReasonCustom ?? config.ControllerMutes.MuteChatReasons[ReasonIndex].Reason));
                RemoveReserved(IDFake);
                FakePlayer FakeP = PlayerBases.FirstOrDefault(x => x.UserID == IDFake);
                if (FakeP != null)
                    PlayerBases.Remove(FakeP);
                return;
            }

            if (!UserInformation.ContainsKey(Target.userID)) return;
            User Info = UserInformation[Target.userID];

            String LangMessage = String.Empty;
            String Reason = String.Empty;
            Int32 MuteTime = 0;

            String NameModerator = GetLang("IQCHAT_FUNCED_ALERT_TITLE_SERVER", Target.UserIDString);

            if (Moderator != null)
            {
                GeneralInformation.RenameInfo ModeratorRename = GeneralInfo.GetInfoRename(Moderator.userID);
                NameModerator = ModeratorRename != null ? $"{ModeratorRename.RenameNick ?? Moderator.displayName}" : Moderator.displayName;
            }

            GeneralInformation.RenameInfo TagetRename = GeneralInfo.GetInfoRename(Target.userID);
            String TargetName = TagetRename != null ? $"{TagetRename.RenameNick ?? Target.displayName}" : Target.displayName;

            if (Target == null || !Target.IsConnected)
            {
                if (Moderator != null && !Command)
                    ReplySystem(Moderator, GetLang("UI_CHAT_PANEL_MODERATOR_MUTE_PANEL_TAKE_TYPE_CHAT_ACTION_NOT_CONNNECTED", Moderator.UserIDString));
                return;
            }
		   		 		  						  	   		  	  			  	  			  	 				  	  	
            if (Moderator != null && !Command)
                if (Info.MuteInfo.IsMute(Type))
                {
                    ReplySystem(Moderator, GetLang("IQCHAT_FUNCED_ALERT_TITLE_ISMUTED", Moderator.UserIDString));
                    return;
                }

            switch (Type)
            {
                case MuteType.Chat:
                    {
                        Reason = ReasonCustom ?? ControllerMutes.MuteChatReasons[ReasonIndex].Reason;
                        MuteTime = TimeCustom == 0 ? ControllerMutes.MuteChatReasons[ReasonIndex].SecondMute : TimeCustom;
                        LangMessage = "FUNC_MESSAGE_MUTE_CHAT";
                        break;
                    }
                case MuteType.Voice:
                    {
                        Reason = ReasonCustom ?? ControllerMutes.MuteVoiceReasons[ReasonIndex].Reason;
                        MuteTime = TimeCustom == 0 ? ControllerMutes.MuteVoiceReasons[ReasonIndex].SecondMute : TimeCustom;
                        LangMessage = "FUNC_MESSAGE_MUTE_VOICE";
                        break;
                    }
            }

            Info.MuteInfo.SetMute(Type, MuteTime);

            if (Moderator != null && Moderator != Target)
                Interface.Oxide.CallHook("OnPlayerMuted", Target, Moderator, MuteTime, Reason);

            if (!HideMute)
                ReplyBroadcast(null, null, false, LangMessage, NameModerator, TargetName, FormatTime(MuteTime, Target.UserIDString), Reason);
               // ReplyBroadcast(GetLang(LangMessage, Target.UserIDString, NameModerator, TargetName, FormatTime(MuteTime, Target.UserIDString), Reason));
            else
            {
                if (Target != null)
                    ReplySystem(Target, GetLang(LangMessage, Target.UserIDString, NameModerator, TargetName, FormatTime(MuteTime, Target.UserIDString), Reason));

                if (Moderator != null)
                    ReplySystem(Moderator, GetLang(LangMessage, Target.UserIDString, NameModerator, TargetName, FormatTime(MuteTime, Target.UserIDString), Reason));
            }

            DiscordLoggMuted(Target, Type, Reason, FormatTime(MuteTime, Target.UserIDString), Moderator);
        }
        private Dictionary<BasePlayer, InformationOpenedUI> LocalBase = new Dictionary<BasePlayer, InformationOpenedUI>();
        public Dictionary<UInt64, AntiNoob> UserInformationConnection = new Dictionary<UInt64, AntiNoob>();
        /// <summary>
        /// Обновление 2.///
        /// Исправления :
        /// - Убран дубликат префикса командного чата
        /// - Исправлена ошибка FormatException после последнего обновления (по моей вине - допустил опечатку в аргументах логирования)
        /// </summary>
        
                [PluginReference] Plugin ImageLibrary, IQFakeActive, IQRankSystem, XLevels, Clans;
        
        
        void API_SEND_PLAYER(BasePlayer player, String PlayerFormat, String Message, String Avatar, Chat.ChatChannel channel = Chat.ChatChannel.Global)
        {
            Configuration.ControllerMessage ControllerMessages = config.ControllerMessages;

            String OutMessage = String.Empty; ;

            if (ControllerMessages.Formatting.FormatMessage)
                OutMessage = $"{Message.ToLower().Substring(0, 1).ToUpper()}{Message.Remove(0, 1).ToLower()}";
		   		 		  						  	   		  	  			  	  			  	 				  	  	
            if (ControllerMessages.Formatting.UseBadWords)
                foreach (String DetectedMessage in OutMessage.Split(' '))
                    if (ControllerMessages.Formatting.BadWords.Contains(DetectedMessage.ToLower()))
                        OutMessage = OutMessage.Replace(DetectedMessage, ControllerMessages.Formatting.ReplaceBadWord);

            player.SendConsoleCommand("chat.add", channel, ulong.Parse(Avatar), $"{PlayerFormat}: {OutMessage}");
            player.ConsoleMessage($"{PlayerFormat}: {OutMessage}");
        }

        
        
        private String XLevel_GetLevel(BasePlayer player)
        {
            if (!XLevels || !config.ReferenceSetting.XLevelsSettings.UseXLevels) return String.Empty;
            return GetLang("XLEVELS_SYNTAX_PREFIX", player.UserIDString,
                (Int32)XLevels?.CallHook("API_GetLevel", player));
        }

        
                private String GetImage(String fileName, UInt64 skin = 0)
        {
            var imageId = (String)plugins.Find("ImageLibrary").CallHook("ImageUi.GetImage", fileName, skin);
            if (!string.IsNullOrEmpty(imageId))
                return imageId;
            return String.Empty;
        }
        
                private void DrawUI_IQChat_Context(BasePlayer player)
        {
            String Interface = InterfaceBuilder.GetInterface("UI_Chat_Context");
            User Info = UserInformation[player.userID];
            Configuration.ControllerParameters ControllerParameter = config.ControllerParameter;
            if (Info == null || ControllerParameter == null || Interface == null) return;

            String BackgroundStatic = IQRankSystem && config.ReferenceSetting.IQRankSystems.UseRankSystem ? "UI_IQCHAT_CONTEXT_RANK" : "UI_IQCHAT_CONTEXT_NO_RANK";
            
            Interface = Interface.Replace("%IMG_BACKGROUND%", ImageUi.GetImage(BackgroundStatic));
            Interface = Interface.Replace("%TITLE%", GetLang("IQCHAT_CONTEXT_TITLE", player.UserIDString));
            Interface = Interface.Replace("%SETTING_ELEMENT%", GetLang("IQCHAT_CONTEXT_SETTING_ELEMENT_TITLE", player.UserIDString));
            Interface = Interface.Replace("%INFORMATION%", GetLang("IQCHAT_CONTEXT_INFORMATION_TITLE", player.UserIDString));
            Interface = Interface.Replace("%SETTINGS%", GetLang("IQCHAT_CONTEXT_SETTINGS_TITLE", player.UserIDString));
            Interface = Interface.Replace("%SETTINGS_PM%", GetLang("IQCHAT_CONTEXT_SETTINGS_PM_TITLE", player.UserIDString));
            Interface = Interface.Replace("%SETTINGS_ALERT%", GetLang("IQCHAT_CONTEXT_SETTINGS_ALERT_TITLE", player.UserIDString));
            Interface = Interface.Replace("%SETTINGS_ALERT_PM%", GetLang("IQCHAT_CONTEXT_SETTINGS_ALERT_PM_TITLE", player.UserIDString));
            Interface = Interface.Replace("%SETTINGS_SOUNDS%", GetLang("IQCHAT_CONTEXT_SETTINGS_SOUNDS_TITLE", player.UserIDString));
            Interface = Interface.Replace("%MUTE_STATUS_TITLE%", GetLang("IQCHAT_CONTEXT_MUTE_STATUS_TITLE", player.UserIDString));
            Interface = Interface.Replace("%IGNORED_STATUS_COUNT%", GetLang("IQCHAT_CONTEXT_IGNORED_STATUS_COUNT", player.UserIDString, Info.Settings.IgnoreUsers.Count));
            Interface = Interface.Replace("%IGNORED_STATUS_TITLE%", GetLang("IQCHAT_CONTEXT_IGNORED_STATUS_TITLE", player.UserIDString));
            Interface = Interface.Replace("%NICK_DISPLAY_TITLE%", GetLang("IQCHAT_CONTEXT_NICK_DISPLAY_TITLE", player.UserIDString));
            Interface = Interface.Replace("%MUTE_STATUS_PLAYER%", Info.MuteInfo.IsMute(MuteType.Chat) ? FormatTime(Info.MuteInfo.GetTime(MuteType.Chat), player.UserIDString) : GetLang("IQCHAT_CONTEXT_MUTE_STATUS_NOT", player.UserIDString));
            Interface = Interface.Replace("%SLIDER_PREFIX_TITLE%", GetLang("IQCHAT_CONTEXT_SLIDER_PREFIX_TITLE", player.UserIDString));
            Interface = Interface.Replace("%SLIDER_NICK_COLOR_TITLE%", GetLang("IQCHAT_CONTEXT_SLIDER_NICK_COLOR_TITLE", player.UserIDString));

            Interface = Interface.Replace("%SLIDER_MESSAGE_COLOR_TITLE%",GetLang("IQCHAT_CONTEXT_SLIDER_MESSAGE_COLOR_TITLE", player.UserIDString));
            
            Interface = Interface.Replace("%SLIDER_IQRANK_TITLE%", IQRankSystem && config.ReferenceSetting.IQRankSystems.UseRankSystem ? GetLang("IQCHAT_CONTEXT_SLIDER_IQRANK_TITLE", player.UserIDString) : String.Empty);

            CuiHelper.DestroyUi(player, InterfaceBuilder.UI_Chat_Context);
            CuiHelper.AddUi(player, Interface);

            DrawUI_IQChat_Update_DisplayName(player);

            if (ControllerParameter.VisualParametres.PrefixType == SelectedParametres.DropList || ControllerParameter.Prefixes.TurnMultiPrefixes)
                DrawUI_IQChat_DropList(player, "-46.788 67.4", "-14.788 91.4", GetLang("IQCHAT_CONTEXT_SLIDER_PREFIX_TITLE_DESCRIPTION", player.UserIDString), ControllerParameter.Prefixes.TurnMultiPrefixes ? TakeElementUser.MultiPrefix : TakeElementUser.Prefix);
            else DrawUI_IQChat_Sliders(player, "SLIDER_PREFIX", "-140 54", "-16 78", TakeElementUser.Prefix);

            if (ControllerParameter.VisualParametres.NickColorType == SelectedParametres.DropList)
                DrawUI_IQChat_DropList(player, "112.34 67.4", "144.34 91.4", GetLang("IQCHAT_CONTEXT_SLIDER_CHAT_NICK_TITLE_DESCRIPTION", player.UserIDString), TakeElementUser.Nick);
            else DrawUI_IQChat_Sliders(player, "SLIDER_NICK_COLOR", "20 54", "144 78", TakeElementUser.Nick);
            
            if (ControllerParameter.VisualParametres.ChatColorType == SelectedParametres.DropList)
                DrawUI_IQChat_DropList(player, "-46.787 -0.591", "-14.787 23.409",GetLang("IQCHAT_CONTEXT_SLIDER_CHAT_MESSAGE_TITLE_DESCRIPTION", player.UserIDString),TakeElementUser.Chat);
            else DrawUI_IQChat_Sliders(player, "SLIDER_MESSAGE_COLOR", "-140 -12", "-16 12", TakeElementUser.Chat);
		   		 		  						  	   		  	  			  	  			  	 				  	  	
            if (IQRankSystem && config.ReferenceSetting.IQRankSystems.UseRankSystem)
            {
                if (ControllerParameter.VisualParametres.IQRankSystemType == SelectedParametres.DropList)
                    DrawUI_IQChat_DropList(player, "112.34 -0.591", "144.34 23.409", GetLang("IQCHAT_CONTEXT_SLIDER_IQRANK_TITLE_DESCRIPTION", player.UserIDString), TakeElementUser.Rank);
                else DrawUI_IQChat_Sliders(player, "SLIDER_IQRANK", "20 -12", "144 12", TakeElementUser.Rank);
            }

            DrawUI_IQChat_Update_Check_Box(player, ElementsSettingsType.PM, "143.38 -67.9", "151.38 -59.9", Info.Settings.TurnPM);
            DrawUI_IQChat_Update_Check_Box(player, ElementsSettingsType.Broadcast, "143.38 -79.6", "151.38 -71.6", Info.Settings.TurnBroadcast);
            DrawUI_IQChat_Update_Check_Box(player, ElementsSettingsType.Alert, "143.38 -91.6", "151.38 -83.6", Info.Settings.TurnAlert);
            DrawUI_IQChat_Update_Check_Box(player, ElementsSettingsType.Sound, "143.38 -103.6", "151.38 -95.6", Info.Settings.TurnSound);
            DrawUI_IQChat_Context_AdminAndModeration(player);
        }
		   		 		  						  	   		  	  			  	  			  	 				  	  	
        
                void OnUserPermissionGranted(string id, string permName) => SetupParametres(id, permName);

        
        
        private List<String> GetPlayersOnline()
        {
            List<String> PlayerNames = new List<String>();
            Int32 Count = 1;

            foreach (BasePlayer playerInList in BasePlayer.activePlayerList.Where(p => !permission.UserHasPermission(p.UserIDString, PermissionHideOnline)))
            {
                String ResultName = $"{Count} - {GetPlayerFormat(playerInList)}";
                PlayerNames.Add(ResultName);

                Count++;
            }

            if (IQFakeActive)
            {
                foreach (FakePlayer fakePlayer in PlayerBases.Where(x => IsFake(x.UserID)))
                {
                    String ResultName = $"{Count} - {API_GET_DEFAULT_PREFIX()}<color={API_GET_DEFAULT_NICK_COLOR()}>{fakePlayer.DisplayName}</color>";
                    PlayerNames.Add(ResultName);

                    Count++;
                }
            }

            return PlayerNames;
        }

        [ConsoleCommand("alertui")]
        private void AlertUIConsoleCommand(ConsoleSystem.Arg args)
        {
            BasePlayer Sender = args.Player();
            if (Sender != null)
                if (!permission.UserHasPermission(Sender.UserIDString, PermissionAlert)) return;
            AlertUI(Sender, args.Args);
        }
        void API_SEND_PLAYER_DISCONNECTED(BasePlayer player, String DisplayName, String reason, String userID)
        {
            Configuration.ControllerAlert.PlayerSession AlertSessionPlayer = config.ControllerAlertSetting.PlayerSessionSetting;

            if (AlertSessionPlayer.DisconnectedAlert)
            {
                String Avatar = AlertSessionPlayer.ConnectedAvatarUse ? userID : String.Empty;

                System.Object[] args = AlertSessionPlayer.DisconnectedReason ? new System.Object[] { DisplayName, reason } : new System.Object[] { DisplayName };
                String Lang = AlertSessionPlayer.DisconnectedReason ? "LEAVE_PLAYER_REASON" : "LEAVE_PLAYER";
                ReplyBroadcast(null, Avatar, false, Lang, args);
            }
        }

        public class Authors
        {
            public string name { get; set; }
            public string url { get; set; }
            public string icon_url { get; set; }
            public string proxy_icon_url { get; set; }
            public Authors(string name, string url, string icon_url, string proxy_icon_url)
            {
                this.name = name;
                this.url = url;
                this.icon_url = icon_url;
                this.proxy_icon_url = proxy_icon_url;
            }
        }
        private void OnServerInitialized()
        {
            _ = this;
            ImageUi.DownloadImages();

            MigrateDataToNoob();

            foreach (BasePlayer player in BasePlayer.activePlayerList)
                UserConnecteionData(player);

            RegisteredPermissions();
            BroadcastAuto();

            CheckValidateUsers();

            if (!config.ControllerMessages.Formatting.ControllerNickname.UseNickController)
                Unsubscribe("OnUserConnected");

        }
        
        void ReplyBroadcast(String CustomPrefix = null, String CustomAvatar = null, Boolean AdminAlert = false, Dictionary<String, List<String>> Messages = null, params object[] args)
        {
            foreach (BasePlayer p in !AdminAlert ? BasePlayer.activePlayerList.Where(p => UserInformation[p.userID].Settings.TurnBroadcast) : BasePlayer.activePlayerList)
            {
                sb.Clear();
                ReplySystem(p, sb.AppendFormat(GetMessages(p, Messages), args).ToString(), CustomPrefix, CustomAvatar);
            }
        }

        
        [ConsoleCommand("set")]
        private void CommandSet(ConsoleSystem.Arg args)
        {
            BasePlayer Sender = args.Player();

            if (Sender != null)
                if (!Sender.IsAdmin)
                    return;
		   		 		  						  	   		  	  			  	  			  	 				  	  	
            if (args == null || args.Args == null || args.Args.Length != 3)
            {
                if (Sender != null)
                    ReplySystem(Sender, LanguageEn ? "Use syntax correctly : set [Steam64ID] [prefix/chat/nick/custom] [Argument]" : "Используйте правильно ситаксис : set [Steam64ID] [prefix/chat/nick/custom] [Argument]");
                else PrintWarning(LanguageEn ? "Use syntax correctly : set [Steam64ID] [prefix/chat/nick/custom] [Argument]" : "Используйте правильно ситаксис : set [Steam64ID] [prefix/chat/nick/custom] [Argument]");
                return;
            }

            UInt64 Steam64ID = 0;
            BasePlayer player = null;

            if (UInt64.TryParse(args.Args[0], out Steam64ID))
                player = BasePlayer.FindByID(Steam64ID);

            if (player == null)
            {
                if (Sender != null)
                    ReplySystem(Sender, LanguageEn ? "Incorrect player Steam ID or syntax error\nUse syntax correctly : set [Steam64ID] [prefix/chat/nick/custom] [Argument]" : "Неверно указан SteamID игрока или ошибка в синтаксисе\nИспользуйте правильно ситаксис : set [Steam64ID] [prefix/chat/nick/custom] [Argument]");
                else PrintWarning(LanguageEn ? "Incorrect player Steam ID or syntax error\nUse syntax correctly : set [Steam64ID] [prefix/chat/nick/custom] [Argument]" : "Неверно указан SteamID игрока или ошибка в синтаксисе\nИспользуйте правильно ситаксис : set [Steam64ID] [prefix/chat/nick/custom] [Argument]");
                return;
            }
            if (!UserInformation.ContainsKey(player.userID))
            {
                if (Sender != null)
                    ReplySystem(Sender, LanguageEn ? $"Player not found!" : $"Игрок не найден!");
                else PrintWarning(LanguageEn ? $"Player not found!" : $"Игрок не найден!");
                return;
            }
            User Info = UserInformation[player.userID];
		   		 		  						  	   		  	  			  	  			  	 				  	  	
            Configuration.ControllerParameters ControllerParameter = config.ControllerParameter;

            switch (args.Args[1])
            {
                case "prefix":
                    {
                        String KeyPrefix = args.Args[2];
                        if (ControllerParameter.Prefixes.Prefixes.Count(prefix => prefix.Argument.Contains(KeyPrefix)) == 0)
                        {
                            if (Sender != null)
                                ReplySystem(Sender, LanguageEn ? "Argument not found in your configuration" : $"Аргумент не найден в вашей конфигурации!");
                            else PrintWarning(LanguageEn ? $"Argument not found in your configuration" : $"Аргумент не найден в вашей конфигурации");
                            return;
                        }

                        foreach (Configuration.ControllerParameters.AdvancedFuncion Prefix in ControllerParameter.Prefixes.Prefixes.Where(prefix => prefix.Argument.Contains(KeyPrefix)).Take(1))
                        {
                            if (ControllerParameter.Prefixes.TurnMultiPrefixes)
                                Info.Info.PrefixList.Add(Prefix.Argument);
                            else Info.Info.Prefix = Prefix.Argument;

                            if (Sender != null)
                                ReplySystem(Sender, LanguageEn ? $"Prefix successfully set to - {Prefix.Argument}" : $"Префикс успешно установлен на - {Prefix.Argument}");
                            else Puts(LanguageEn ? $"Prefix successfully set to - {Prefix.Argument}" : $"Префикс успешно установлен на - {Prefix.Argument}");
                        }
                        break;
                    }
                case "chat":
                    {
                        String KeyChatColor = args.Args[2];
                        if (ControllerParameter.MessageColorList.Count(color => color.Argument.Contains(KeyChatColor)) == 0)
                        {
                            if (Sender != null)
                                ReplySystem(Sender, LanguageEn ? $"Argument not found in your configuration!" : $"Аргумент не найден в вашей конфигурации!");
                            else PrintWarning(LanguageEn ? $"Argument not found in your configuration" : $"Аргумент не найден в вашей конфигурации");
                            return;
                        }

                        foreach (Configuration.ControllerParameters.AdvancedFuncion ChatColor in ControllerParameter.MessageColorList.Where(color => color.Argument.Contains(KeyChatColor)).Take(1))
                        {
                            Info.Info.ColorMessage = ChatColor.Argument;
                            if (Sender != null)
                                ReplySystem(Sender, LanguageEn ? $"Message color successfully set to - {ChatColor.Argument}" : $"Цвет сообщения успешно установлен на - {ChatColor.Argument}");
                            else Puts(LanguageEn ? $"Message color successfully set to - {ChatColor.Argument}" : $"Цвет сообщения успешно установлен на - {ChatColor.Argument}");
                        }
                        break;
                    }
                case "nick":
                    {
                        String KeyNickColor = args.Args[2];
                        if (ControllerParameter.NickColorList.Count(color => color.Argument.Contains(KeyNickColor)) == 0)
                        {
                            if (Sender != null)
                                ReplySystem(Sender, LanguageEn ? $"Argument not found in your configuration!" : $"Аргумент не найден в вашей конфигурации!");
                            else PrintWarning(LanguageEn ? "Argument not found in your configuration" : $"Аргумент не найден в вашей конфигурации");
                            return;
                        }

                        foreach (Configuration.ControllerParameters.AdvancedFuncion NickColor in ControllerParameter.NickColorList.Where(color => color.Argument.Contains(KeyNickColor)).Take(1))
                        {
                            Info.Info.ColorNick = NickColor.Argument;
                            if (Sender != null)
                                ReplySystem(Sender, LanguageEn ? $"Message color successfully set to - {NickColor.Argument}" : $"Цвет сообщения успешно установлен на - {NickColor.Argument}");
                            else Puts(LanguageEn ? $"Message color successfully set to - {NickColor.Argument}" : $"Цвет сообщения успешно установлен на - {NickColor.Argument}");
                        }
                        break;
                    }
                case "custom":
                    {
                        String CustomPrefix = args.Args[2];
                        if (ControllerParameter.Prefixes.TurnMultiPrefixes)
                            Info.Info.PrefixList.Add(CustomPrefix);
                        else Info.Info.Prefix = CustomPrefix;
                        if (Sender != null)
                            ReplySystem(Sender, LanguageEn ? $"Custom prefix successfully set to - {CustomPrefix}" : $"Кастомный префикс успешно установлен на - {CustomPrefix}");
                        else Puts(LanguageEn ? $"Custom prefix successfully set to - {CustomPrefix}" : $"Кастомный префикс успешно установлен на - {CustomPrefix}");

                        break;
                    }
                default:
                    {
                        if (Sender != null)
                            ReplySystem(Sender, LanguageEn ? "Use syntax correctly : set [Steam64ID] [prefix/chat/nick/custom] [Argument]" : "Используйте правильно ситаксис : set [Steam64ID] [prefix/chat/nick/custom] [Argument]");
                        break;
                    }
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

        private object OnServerMessage(String message, String name)
        {
            if (config.ControllerMessages.TurnedFunc.HideAdminGave)
                if (message.Contains("gave") && name == "SERVER")
                    return true;
            return null;
        }
        void IQRankSetRank(ulong userID, string RankKey) => IQRankSystem?.Call("API_SET_ACTIVE_RANK", userID, RankKey);

        public class GeneralInformation
        {
            public Boolean TurnMuteAllChat;
            public Boolean TurnMuteAllVoice;

            public Dictionary<UInt64, RenameInfo> RenameList = new Dictionary<UInt64, RenameInfo>();
            internal class RenameInfo
            {
                public String RenameNick;
                public UInt64 RenameID;
            }

            public RenameInfo GetInfoRename(UInt64 UserID)
            {
                if (!RenameList.ContainsKey(UserID)) return null;
                return RenameList[UserID];
            }
        }


        private Tuple<String, Boolean> BadWordsCleaner(String FormattingMessage, String ReplaceBadWord, List<String> BadWords)
        {
            String ResultMessage = FormattingMessage;
            Boolean IsBadWords = false;

            foreach (String word in BadWords.Where(x => !x.Contains("*")))
            {
                MatchCollection matches = new Regex(@"\b(" + Regex.Escape(word) + @")\b").Matches(ResultMessage);
		   		 		  						  	   		  	  			  	  			  	 				  	  	
                foreach (Match match in matches)
                {
                    if (match.Success)
                    {
                        String found = match.Groups[1].ToString();
                        String replaced = "";

                        for (int i = 0; i < found.Length; i++) replaced = replaced + ReplaceBadWord;

                        ResultMessage = ResultMessage.Replace(found, replaced);
                        IsBadWords = true;
                    }
                    else break;
                }
            }

            return Tuple.Create(ResultMessage, IsBadWords);
        }

        private void DrawUI_IQChat_Mute_Alert_Reasons(BasePlayer player, BasePlayer Target, String Reason, Int32 Y, MuteType Type, UInt64 IDFake = 0)
        {
            String Interface = InterfaceBuilder.GetInterface("UI_Chat_Mute_Alert_DropList_Reason");
            if (Interface == null) return;

            Interface = Interface.Replace("%OFFSET_MIN%", $"-147.5 {85.42 - (Y * 40)}");
            Interface = Interface.Replace("%OFFSET_MAX%", $"147.5 {120.42 - (Y * 40)}");
            Interface = Interface.Replace("%REASON%", Reason);
            Interface = Interface.Replace("%COMMAND_REASON%", $"newui.cmd action.mute.ignore ignore.and.mute.controller {SelectedAction.Mute} confirm.yes {((IQFakeActive && Target == null && IDFake != 0) ? IDFake : Target.userID)} {Type} {Y}");
            CuiHelper.AddUi(player, Interface);
        }

        void ReadData()
        {
            if (!Oxide.Core.Interface.Oxide.DataFileSystem.ExistsDatafile("IQSystem/IQChat/Users") && Oxide.Core.Interface.Oxide.DataFileSystem.ExistsDatafile("IQChat/Users"))
            {
                GeneralInfo = Oxide.Core.Interface.Oxide.DataFileSystem.ReadObject<GeneralInformation>("IQChat/Information");
                UserInformation = Oxide.Core.Interface.Oxide.DataFileSystem.ReadObject<Dictionary<UInt64, User>>("IQChat/Users");

                Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("IQSystem/IQChat/Information", GeneralInfo);
                Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("IQSystem/IQChat/Users", UserInformation);

                PrintWarning(LanguageEn ? "Your player data has been moved to a new directory - IQSystem/IQChat , you can delete old data files!" : "Ваши данные игроков были перенесены в новую директорию - IQSystem/IQChat , вы можете удалить старые дата-файлы!");
            }

            GeneralInfo = Oxide.Core.Interface.Oxide.DataFileSystem.ReadObject<GeneralInformation>("IQSystem/IQChat/Information");
            UserInformation = Oxide.Core.Interface.Oxide.DataFileSystem.ReadObject<Dictionary<UInt64, User>>("IQSystem/IQChat/Users");
            UserInformationConnection = Oxide.Core.Interface.Oxide.DataFileSystem.ReadObject<Dictionary<UInt64, AntiNoob>>("IQSystem/IQChat/AntiNoob");
        }
        void API_SEND_PLAYER_PM(BasePlayer player, string DisplayName, string Message)
        {
            ReplySystem(player, GetLang("COMMAND_PM_SEND_MSG", player.UserIDString, DisplayName, Message));
		   		 		  						  	   		  	  			  	  			  	 				  	  	
            if (UserInformation.ContainsKey(player.userID))
                if (UserInformation[player.userID].Settings.TurnSound)
                    Effect.server.Run(config.ControllerMessages.TurnedFunc.PMSetting.SoundPM, player.GetNetworkPosition());
        }

        public class Footer
        {
            public string text { get; set; }
            public string icon_url { get; set; }
            public string proxy_icon_url { get; set; }
            public Footer(string text, string icon_url, string proxy_icon_url)
            {
                this.text = text;
                this.icon_url = icon_url;
                this.proxy_icon_url = proxy_icon_url;
            }
        }

        private static IQChat _;
        void Alert(BasePlayer Sender, BasePlayer Recipient, string[] arg)
        {
            String Message = GetMessageInArgs(Sender, arg);
            if (Message == null) return;

            ReplySystem(Recipient, Message);
        }
        public bool IsFake(UInt64 userID)
        {
            if (!IQFakeActive) return false;
            return (bool)IQFakeActive?.Call("IsFake", userID);
        }
        private String GetMessageInArgs(BasePlayer Sender, String[] arg)
        {
            if (arg == null || arg.Length == 0)
            {
                if (Sender != null)
                    ReplySystem(Sender, GetLang("FUNC_MESSAGE_NO_ARG_BROADCAST", Sender.UserIDString));
                else PrintWarning(GetLang("FUNC_MESSAGE_NO_ARG_BROADCAST"));
                return null;
            }
            String Message = String.Empty;
            foreach (String msg in arg)
                Message += " " + msg;

            return Message;
        }

        private void RemoveParametres(String ID, String Permissions)
        {
            UInt64 UserID = UInt64.Parse(ID);
            BasePlayer player = BasePlayer.FindByID(UserID);
		   		 		  						  	   		  	  			  	  			  	 				  	  	
            Configuration.ControllerConnection Controller = config.ControllerConnect;
            Configuration.ControllerParameters Parameters = config.ControllerParameter;

            if (!UserInformation.ContainsKey(UserID)) return;
            User Info = UserInformation[UserID];

            if (Controller.Turneds.TurnAutoDropPrefix)
            {
                if (Parameters.Prefixes.TurnMultiPrefixes)
                {
                    foreach (Configuration.ControllerParameters.AdvancedFuncion Prefixes in
                             Parameters.Prefixes.Prefixes.Where(prefix =>
                                 Info.Info.PrefixList.Contains(prefix.Argument) && prefix.Permissions == Permissions))
                    {
                        Info.Info.PrefixList.Remove(Prefixes.Argument);

                        if (player != null)
                            ReplySystem(player, GetLang("PREFIX_RETURNRED", player.UserIDString, Prefixes.Argument));

                        Log(LanguageEn
                            ? $"Player ({UserID}) expired prefix {Prefixes.Argument}"
                            : $"У игрока ({UserID}) истек префикс {Prefixes.Argument}");
                    }
                }
                else
                {
                    Configuration.ControllerParameters.AdvancedFuncion Prefixes = Parameters.Prefixes.Prefixes.FirstOrDefault(prefix => prefix.Argument == Info.Info.Prefix && prefix.Permissions == Permissions);
                    if (Prefixes != null)
                    {
                        Info.Info.Prefix = Controller.SetupDefaults.PrefixDefault;

                        if (player != null)
                            ReplySystem(player, GetLang("PREFIX_RETURNRED", player.UserIDString, Prefixes.Argument));

                        Log(LanguageEn
                            ? $"Player ({UserID}) expired prefix {Prefixes.Argument}"
                            : $"У игрока ({UserID}) истек префикс {Prefixes.Argument}");
                    }
                }
            }
            if (Controller.Turneds.TurnAutoSetupColorNick)
            {
                Configuration.ControllerParameters.AdvancedFuncion ColorNick = Parameters.NickColorList.FirstOrDefault(nick => Info.Info.ColorNick == nick.Argument && nick.Permissions == Permissions);
                if (ColorNick != null)
                {
                    Info.Info.ColorNick = Controller.SetupDefaults.NickDefault;
		   		 		  						  	   		  	  			  	  			  	 				  	  	
                    if (player != null)
                        ReplySystem(player, GetLang("COLOR_NICK_RETURNRED", player.UserIDString, ColorNick.Argument));
		   		 		  						  	   		  	  			  	  			  	 				  	  	
                    Log(LanguageEn
                        ? $"Player ({UserID}) expired nick color {ColorNick.Argument}"
                        : $"У игрока ({UserID}) истек цвет ника {ColorNick.Argument}");
                }
            }
            if (Controller.Turneds.TurnAutoSetupColorChat)
            {
                Configuration.ControllerParameters.AdvancedFuncion ColorChat = Parameters.MessageColorList.FirstOrDefault(message => Info.Info.ColorMessage == message.Argument && message.Permissions == Permissions);
                if (ColorChat == null) return;

                Info.Info.ColorMessage = Controller.SetupDefaults.MessageDefault;

                if (player != null)
                    ReplySystem(player, GetLang("COLOR_CHAT_RETURNRED", player.UserIDString, ColorChat.Argument));

                Log(LanguageEn ? $"Player ({UserID}) chat color expired {ColorChat.Argument}" : $"У игрока ({UserID}) истек цвет чата {ColorChat.Argument}");
            }
        }
        private void UserConnecteionData(BasePlayer player)
        {
            if (config.ControllerMessages.TurnedFunc.AntiNoobSetting.AntiNoobPM.AntiNoobActivate || config.ControllerMessages.TurnedFunc.AntiNoobSetting.AntiNoobChat.AntiNoobActivate)
            {
                if (!UserInformationConnection.ContainsKey(player.userID))
                    UserInformationConnection.Add(player.userID, new AntiNoob());
            }
		   		 		  						  	   		  	  			  	  			  	 				  	  	
            Configuration.ControllerConnection ControllerConntect = config.ControllerConnect;
            Configuration.ControllerParameters ControllerParameter = config.ControllerParameter;
            if (ControllerConntect == null || ControllerParameter == null || UserInformation.ContainsKey(player.userID)) return;

            User Info = new User();
            if (ControllerConntect.Turneds.TurnAutoSetupPrefix)
            {
                if (ControllerParameter.Prefixes.TurnMultiPrefixes)
                    Info.Info.PrefixList.Add(ControllerConntect.SetupDefaults.PrefixDefault ?? "");
                else Info.Info.Prefix = ControllerConntect.SetupDefaults.PrefixDefault ?? "";
            }

            if (ControllerConntect.Turneds.TurnAutoSetupColorNick)
                Info.Info.ColorNick = ControllerConntect.SetupDefaults.NickDefault;

            if (ControllerConntect.Turneds.TurnAutoSetupColorChat)
                Info.Info.ColorMessage = ControllerConntect.SetupDefaults.MessageDefault;

            Info.Info.Rank = String.Empty;

            UserInformation.Add(player.userID, Info);
        }
        
        
                [ConsoleCommand("newui.cmd")]
        private void ConsoleCommandFuncional(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;
            String Action = arg.Args[0];
            if (Action == null || String.IsNullOrWhiteSpace(Action)) return;

            if (!LocalBase.ContainsKey(player))
            {
                PrintError(LanguageEn ? "UI was unable to process the local base (Local Base) contact the developer" : "UI не смог обработать локальную базу (LocalBase) свяжитесь с разработчиком");
                return;
            }
            Configuration.ControllerParameters ControllerParameters = config.ControllerParameter;
            if (ControllerParameters == null)
            {
                PrintError(LanguageEn ? "An error has been made in the configuration! Controller Parameters is null, contact developer" : "В конфигурации допущена ошибка! ControllerParameters является null, свяжитесь с разработчиком");
                return;
            }

            switch (Action)
            {
                case "action.mute.ignore":
                    {
                        String ActionMenu = arg.Args[1];
                        SelectedAction ActionType = (SelectedAction)Enum.Parse(typeof(SelectedAction), arg.Args[2]);
                        if (ActionMenu == "search.controller" && arg.Args.Length < 4)
                            return;

                        switch (ActionMenu)
                        {
                            case "mute.controller":
                                {
                                    if (!player.IsAdmin)
                                        if (!permission.UserHasPermission(player.UserIDString, PermissionMute)) return;
		   		 		  						  	   		  	  			  	  			  	 				  	  	
                                    String ActionMute = arg.Args[3];
                                    switch (ActionMute)
                                    {
                                        case "mute.all.chat":
                                            {
                                                if (GeneralInfo.TurnMuteAllChat)
                                                {
                                                    GeneralInfo.TurnMuteAllChat = false;
                                                  //  ReplyBroadcast(GetLang("IQCHAT_FUNCED_NO_SEND_CHAT_UNMUTED_ALL_CHAT", player.UserIDString), AdminAlert: true);
                                                    ReplyBroadcast(null, null, true, "IQCHAT_FUNCED_NO_SEND_CHAT_UNMUTED_ALL_CHAT");
                                                }
                                                else
                                                {
                                                    GeneralInfo.TurnMuteAllChat = true;
                                                   // ReplyBroadcast(GetLang("IQCHAT_FUNCED_NO_SEND_CHAT_MUTED_ALL_CHAT", player.UserIDString), AdminAlert: true);
                                                    ReplyBroadcast(null, null, true, "IQCHAT_FUNCED_NO_SEND_CHAT_MUTED_ALL_CHAT");
                                                }

                                                DrawUI_IQChat_Update_MuteChat_All(player);
                                                break;
                                            }
                                        case "mute.all.voice":
                                            {
                                                if (GeneralInfo.TurnMuteAllVoice)
                                                {
                                                    GeneralInfo.TurnMuteAllVoice = false;
                                                 //   ReplyBroadcast(GetLang("IQCHAT_FUNCED_NO_SEND_CHAT_UMMUTED_ALL_VOICE", player.UserIDString), AdminAlert: true);
                                                    ReplyBroadcast(null, null, true, "IQCHAT_FUNCED_NO_SEND_CHAT_UMMUTED_ALL_VOICE");
                                                }
                                                else
                                                {
                                                    GeneralInfo.TurnMuteAllVoice = true;
                                                   // ReplyBroadcast(GetLang("IQCHAT_FUNCED_NO_SEND_CHAT_MUTED_ALL_VOICE", player.UserIDString), AdminAlert: true);
                                                    ReplyBroadcast(null, null, true, "IQCHAT_FUNCED_NO_SEND_CHAT_MUTED_ALL_VOICE");

                                                }
                                                DrawUI_IQChat_Update_MuteVoice_All(player);
                                                break;
                                            }
                                        default:
                                            break;
                                    }
                                    break;
                                }
                            case "ignore.and.mute.controller":
                                {
                                    String ActionController = arg.Args[3];
                                    BasePlayer TargetPlayer = BasePlayer.Find(arg.Args[4]);
                                    UInt64 ID = 0;
                                    UInt64.TryParse(arg.Args[4], out ID);

                                    if (TargetPlayer == null && !IsFake(ID))
                                    {
                                        CuiHelper.DestroyUi(player, "MUTE_AND_IGNORE_PANEL_ALERT");
                                        return;
                                    }

                                    switch (ActionController)
                                    {
                                        case "confirm.alert":
                                            {
                                                if (ActionType == SelectedAction.Ignore)
                                                    DrawUI_IQChat_Ignore_Alert(player, TargetPlayer, ID);
                                                else DrawUI_IQChat_Mute_Alert(player, TargetPlayer, ID);
                                                break;
                                            }
                                        case "open.reason.mute":
                                            {
                                                MuteType Type = (MuteType)Enum.Parse(typeof(MuteType), arg.Args[5]);
                                                DrawUI_IQChat_Mute_Alert_Reasons(player, TargetPlayer, Type, IDFake: ID);
                                                break;
                                            }
                                        case "confirm.yes":
                                            {
                                                if (ActionType == SelectedAction.Ignore)
                                                {
                                                    User Info = UserInformation[player.userID];
                                                    Info.Settings.IgnoredAddOrRemove(IsFake(ID) ? ID : TargetPlayer.userID);

                                                    CuiHelper.DestroyUi(player, "MUTE_AND_IGNORE_PANEL_ALERT");
                                                    DrawUI_IQChat_Mute_And_Ignore_Player_Panel(player, ActionType);
                                                }
                                                else
                                                {
                                                    MuteType Type = (MuteType)Enum.Parse(typeof(MuteType), arg.Args[5]);
                                                    Int32 IndexReason = Int32.Parse(arg.Args[6]);
		   		 		  						  	   		  	  			  	  			  	 				  	  	
                                                    MutePlayer(TargetPlayer, Type, IndexReason, player, IDFake: ID);

                                                    CuiHelper.DestroyUi(player, "MUTE_AND_IGNORE_PANEL_ALERT");
                                                    DrawUI_IQChat_Mute_And_Ignore_Player_Panel(player, ActionType);
                                                }
                                                break;
                                            }
                                        case "unmute.yes":
                                            {
                                                MuteType Type = (MuteType)Enum.Parse(typeof(MuteType), arg.Args[5]);

                                                UnmutePlayer(TargetPlayer, Type, player);

                                                CuiHelper.DestroyUi(player, "MUTE_AND_IGNORE_PANEL_ALERT");
                                                DrawUI_IQChat_Mute_And_Ignore_Player_Panel(player, ActionType);
                                                break;
                                            }
                                    }
                                    break;
                                }
                            case "open":
                                {
                                    DrawUI_IQChat_Mute_And_Ignore(player, ActionType);
                                    break;
                                }
                            case "page.controller":
                                {
                                    Int32 Page = Int32.Parse(arg.Args[3]);

                                    DrawUI_IQChat_Mute_And_Ignore_Player_Panel(player, ActionType, Page);
                                    break;
                                }
                            case "search.controller":
                                {
                                    String SearchName = arg.Args[3];
                                    DrawUI_IQChat_Mute_And_Ignore_Player_Panel(player, ActionType, SearchName: SearchName);
                                    break;
                                }
                            default:
                                break;
                        }

                        break;
                    }
                case "checkbox.controller":
                    {
                        ElementsSettingsType Type = (ElementsSettingsType)Enum.Parse(typeof(ElementsSettingsType), arg.Args[1]);
                        if (!UserInformation.ContainsKey(player.userID)) return;
                        User Info = UserInformation[player.userID];
                        if (Info == null) return;

                        switch (Type)
                        {
                            case ElementsSettingsType.PM:
                                {
                                    if (Info.Settings.TurnPM)
                                        Info.Settings.TurnPM = false;
                                    else Info.Settings.TurnPM = true;

                                    DrawUI_IQChat_Update_Check_Box(player, Type, "143.38 -67.9", "151.38 -59.9", Info.Settings.TurnPM);
                                    break;
                                }
                            case ElementsSettingsType.Broadcast:
                                {
                                    if (Info.Settings.TurnBroadcast)
                                        Info.Settings.TurnBroadcast = false;
                                    else Info.Settings.TurnBroadcast = true;

                                    DrawUI_IQChat_Update_Check_Box(player, Type, "143.38 -79.6", "151.38 -71.6", Info.Settings.TurnBroadcast);
                                    break;
                                }
                            case ElementsSettingsType.Alert:
                                {
                                    if (Info.Settings.TurnAlert)
                                        Info.Settings.TurnAlert = false;
                                    else Info.Settings.TurnAlert = true;
		   		 		  						  	   		  	  			  	  			  	 				  	  	
                                    DrawUI_IQChat_Update_Check_Box(player, Type, "143.38 -91.6", "151.38 -83.6", Info.Settings.TurnAlert);
                                    break;
                                }
                            case ElementsSettingsType.Sound:
                                {
                                    if (Info.Settings.TurnSound)
                                        Info.Settings.TurnSound = false;
                                    else Info.Settings.TurnSound = true;

                                    DrawUI_IQChat_Update_Check_Box(player, Type, "143.38 -103.6", "151.38 -95.6", Info.Settings.TurnSound);
                                    break;
                                }
                            default:
                                break;
                        }
                        break;
                    }
                case "droplist.controller":
                    {
                        String ActionDropList = arg.Args[1];
                        TakeElementUser Element = (TakeElementUser)Enum.Parse(typeof(TakeElementUser), arg.Args[2]);

                        switch (ActionDropList)
                        {
                            case "open":
                                {
                                    DrawUI_IQChat_OpenDropList(player, Element);
                                    break;
                                }
                            case "page.controller":
                                {
                                    String ActionDropListPage = arg.Args[3];
                                    Int32 Page = (Int32)Int32.Parse(arg.Args[4]);
                                    Page = ActionDropListPage == "+" ? Page + 1 : Page - 1;

                                    DrawUI_IQChat_OpenDropList(player, Element, Page);
                                    break;
                                }
                            case "element.take":
                                {
                                    Int32 Count = Int32.Parse(arg.Args[3]);
                                    String Permissions = arg.Args[4];
                                    String Argument = String.Join(" ", arg.Args.Skip(5));
                                    if (!permission.UserHasPermission(player.UserIDString, Permissions)) return;
                                    if (!UserInformation.ContainsKey(player.userID)) return;
                                    User User = UserInformation[player.userID];
                                    if (User == null) return;

                                    switch (Element)
                                    {
                                        case TakeElementUser.MultiPrefix:
                                            {
                                                if (!User.Info.PrefixList.Contains(Argument))
                                                {
                                                    User.Info.PrefixList.Add(Argument);
                                                    DrawUI_IQChat_OpenDropListArgument(player, Count);
                                                }
                                                else
                                                {
                                                    User.Info.PrefixList.Remove(Argument);
                                                    CuiHelper.DestroyUi(player, $"TAKED_INFO_{Count}");
                                                }
                                                break;
                                            }
                                        case TakeElementUser.Prefix:
                                            User.Info.Prefix = User.Info.Prefix.Equals(Argument) ? String.Empty : Argument;
                                            break;
                                        case TakeElementUser.Nick:
                                            User.Info.ColorNick = Argument;
                                            break;
                                        case TakeElementUser.Chat:
                                            User.Info.ColorMessage = Argument;
                                            break;
                                        case TakeElementUser.Rank:
                                            {
                                                User.Info.Rank = Argument;
                                                IQRankSetRank(player.userID, Argument);
                                            }
                                            break;
                                        default:
                                            break;
                                    }
                                    DrawUI_IQChat_Update_DisplayName(player);
                                    break;
                                }
                        }
                        break;
                    }
                case "slider.controller": // newui.cmd slider.controller 0 +
                    {
                        TakeElementUser Element = (TakeElementUser)Enum.Parse(typeof(TakeElementUser), arg.Args[1]);
                        List<Configuration.ControllerParameters.AdvancedFuncion> SliderElements = new List<Configuration.ControllerParameters.AdvancedFuncion>();
                        User Info = UserInformation[player.userID];
                        if (Info == null) return;

                        InformationOpenedUI InfoUI = LocalBase[player];
                        if (InfoUI == null) return;


                        String ActionSlide = arg.Args[2];

                        switch (Element)
                        {
                            case TakeElementUser.Prefix:
                                {
                                    SliderElements = LocalBase[player].ElementsPrefix;
		   		 		  						  	   		  	  			  	  			  	 				  	  	
                                    if (SliderElements == null || SliderElements.Count == 0) return;

                                    if (ActionSlide == "+")
                                    {
                                        InfoUI.SlideIndexPrefix++;

                                        if (InfoUI.SlideIndexPrefix >= SliderElements.Count)
                                            InfoUI.SlideIndexPrefix = 0;
                                    }
                                    else
                                    {
                                        InfoUI.SlideIndexPrefix--;

                                        if (InfoUI.SlideIndexPrefix < 0)
                                            InfoUI.SlideIndexPrefix = SliderElements.Count - 1;
                                    }

                                    Info.Info.Prefix = SliderElements[InfoUI.SlideIndexPrefix].Argument;
                                }
                                break;
                            case TakeElementUser.Nick:
                                {
                                    SliderElements = LocalBase[player].ElementsNick;

                                    if (SliderElements == null || SliderElements.Count == 0) return;

                                    if (ActionSlide == "+")
                                    {
                                        InfoUI.SlideIndexNick++;

                                        if (InfoUI.SlideIndexNick >= SliderElements.Count)
                                            InfoUI.SlideIndexNick = 0;
                                    }
                                    else
                                    {
                                        InfoUI.SlideIndexNick--;

                                        if (InfoUI.SlideIndexNick < 0)
                                            InfoUI.SlideIndexNick = SliderElements.Count - 1;
                                    }
                                    Info.Info.ColorNick = SliderElements[InfoUI.SlideIndexNick].Argument;
                                }
                                break;
                            case TakeElementUser.Chat:
                                {
                                    SliderElements = LocalBase[player].ElementsChat;
                                    if (SliderElements == null || SliderElements.Count == 0) return;

                                    if (ActionSlide == "+")
                                    {
                                        InfoUI.SlideIndexChat++;

                                        if (InfoUI.SlideIndexChat >= SliderElements.Count)
                                            InfoUI.SlideIndexChat = 0;
                                    }
                                    else
                                    {
                                        InfoUI.SlideIndexChat--;
		   		 		  						  	   		  	  			  	  			  	 				  	  	
                                        if (InfoUI.SlideIndexChat < 0)
                                            InfoUI.SlideIndexChat = SliderElements.Count - 1;
                                    }
                                    Info.Info.ColorMessage = SliderElements[InfoUI.SlideIndexChat].Argument;
                                }
                                break;
                            case TakeElementUser.Rank:
                                {
                                    SliderElements = LocalBase[player].ElementsRanks;
                                    if (SliderElements == null || SliderElements.Count == 0) return;

                                    if (ActionSlide == "+")
                                    {
                                        InfoUI.SlideIndexRank++;

                                        if (InfoUI.SlideIndexRank >= SliderElements.Count)
                                            InfoUI.SlideIndexRank = 0;
                                    }
                                    else
                                    {
                                        InfoUI.SlideIndexRank--;

                                        if (InfoUI.SlideIndexRank < 0)
                                            InfoUI.SlideIndexRank = SliderElements.Count - 1;
                                    }
                                    Info.Info.Rank = SliderElements[InfoUI.SlideIndexRank].Argument;
                                    IQRankSetRank(player.userID, SliderElements[InfoUI.SlideIndexRank].Argument);
                                }
                                break;
                            default:
                                break;
                        }
                        DrawUI_IQChat_Slider_Update_Argument(player, Element);
                        DrawUI_IQChat_Update_DisplayName(player);
                        break;
                    }
                default:
                    break;
            }
        }
        private void AddHistoryMessage(BasePlayer player, String Message)
        {
            if (!LastMessagesChat.ContainsKey(player))
                LastMessagesChat.Add(player, new List<String> { Message });
            else LastMessagesChat[player].Add(Message);
        }

        private String GetPlayerFormat(BasePlayer playerInList)
        {
            GeneralInformation.RenameInfo Renamer = GeneralInfo.GetInfoRename(playerInList.userID);
            String NickNamed = Renamer != null ? $"{Renamer.RenameNick ?? playerInList.displayName}" : playerInList.displayName;

            User Info = UserInformation[playerInList.userID];

            Configuration.ControllerParameters ControllerParameter = config.ControllerParameter;
            Configuration.ControllerMute ControllerMutes = config.ControllerMutes;

            String Prefixes = String.Empty;
            String ColorNickPlayer = String.IsNullOrWhiteSpace(Info.Info.ColorNick) ? playerInList.IsAdmin ? "#a8fc55" : "#54aafe" : Info.Info.ColorNick;

            if (ControllerParameter.Prefixes.TurnMultiPrefixes)
            {
                if (Info.Info.PrefixList != null)
                    Prefixes = String.Join("", Info.Info.PrefixList.Take(ControllerParameter.Prefixes.MaximumMultiPrefixCount));
            }
            else Prefixes = Info.Info.Prefix;

            String ResultName = $"{Prefixes}<color={ColorNickPlayer}>{NickNamed}</color>";

            return ResultName;
        }
        
        
        object OnPlayerVoice(BasePlayer player, Byte[] data)
        {
            if (UserInformation[player.userID].MuteInfo.IsMute(MuteType.Voice))
                return false;
            return null;
        }
		   		 		  						  	   		  	  			  	  			  	 				  	  	
                private void DrawUI_IQChat_Update_DisplayName(BasePlayer player)
        {
            String InterfaceVisualNick = InterfaceBuilder.GetInterface("UI_Chat_Context_Visual_Nick");
            User Info = UserInformation[player.userID];
            Configuration.ControllerParameters Controller = config.ControllerParameter;
            if (Info == null || InterfaceVisualNick == null || Controller == null) return;

            String DisplayNick = String.Empty;

            String Pattern = @"</?size.*?>";
            // if (Controller.Prefixes.TurnMultiPrefixes) 
            // {
            //     if (Info.Info.PrefixList != null && Info.Info.PrefixList.Count != 0)
            //         DisplayNick += Info.Info.PrefixList.Count > 1 ? $"{(Regex.IsMatch(Info.Info.PrefixList[0], Pattern) ? Regex.Replace(Info.Info.PrefixList[0], Pattern, "") : Info.Info.PrefixList[0])}+{Info.Info.PrefixList.Count - 1}" :
            //             (Regex.IsMatch(Info.Info.PrefixList[0], Pattern) ? Regex.Replace(Info.Info.PrefixList[0], Pattern, "") : Info.Info.PrefixList[0]);
            // }
            // else DisplayNick += Regex.IsMatch(Info.Info.Prefix, Pattern) ? Regex.Replace(Info.Info.Prefix, Pattern, "") : Info.Info.Prefix;
            //
            if (Controller.Prefixes.TurnMultiPrefixes)
            {
                if (Info.Info.PrefixList != null && Info.Info.PrefixList.Count != 0)
                {
                    if (Info.Info.PrefixList[0] != null && Regex.IsMatch(Info.Info.PrefixList[0], Pattern))
                        DisplayNick += Regex.Replace(Info.Info.PrefixList[0], Pattern, "");
                    else
                        DisplayNick += Info.Info.PrefixList[0];

                    DisplayNick += Info.Info.PrefixList.Count > 1 ? $"+{Info.Info.PrefixList.Count - 1}" : string.Empty;
                }
            }
            else
            {
                if (Info.Info.Prefix != null && Regex.IsMatch(Info.Info.Prefix, Pattern))
                    DisplayNick += Regex.Replace(Info.Info.Prefix, Pattern, "");
                else DisplayNick += Info.Info.Prefix;
            }

            DisplayNick += $"<color={Info.Info.ColorNick ?? "#ffffff"}>{player.displayName}</color>: <color={Info.Info.ColorMessage ?? "#ffffff"}>{GetLang("IQCHAT_CONTEXT_NICK_DISPLAY_MESSAGE", player.UserIDString)}</color>";

            InterfaceVisualNick = InterfaceVisualNick.Replace("%NICK_DISPLAY%", DisplayNick);


            CuiHelper.DestroyUi(player, InterfaceBuilder.UI_Chat_Context_Visual_Nick);
            CuiHelper.AddUi(player, InterfaceVisualNick);
        }

        
        private static Configuration config = new Configuration();
        void WriteData()
        {
            Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("IQSystem/IQChat/Information", GeneralInfo);
            Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("IQSystem/IQChat/Users", UserInformation);
            Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("IQSystem/IQChat/AntiNoob", UserInformationConnection);
        }

        
        
                private new void LoadDefaultMessages()
        {
            PrintWarning(LanguageEn ? "Language file is loading..." : "Языковой файл загружается...");
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["FUNC_MESSAGE_MUTE_CHAT"] = "{0} muted {1}\nDuration : {2}\nReason : {3}",
                ["FUNC_MESSAGE_UNMUTE_CHAT"] = "{0} unmuted {1}",
                ["FUNC_MESSAGE_MUTE_VOICE"] = "{0} muted voice to {1}\nDuration : {2}\nReason : {3}",
                ["FUNC_MESSAGE_UNMUTE_VOICE"] = "{0} unmuted voice to {1}",
                ["FUNC_MESSAGE_MUTE_ALL_CHAT"] = "Chat disabled",
                ["FUNC_MESSAGE_UNMUTE_ALL_CHAT"] = "Chat enabled",
                ["FUNC_MESSAGE_MUTE_ALL_VOICE"] = "Voice chat disabled",
                ["FUNC_MESSAGE_UNMUTE_ALL_VOICE"] = "Voice chat enabled",
                ["FUNC_MESSAGE_MUTE_ALL_ALERT"] = "Blocking by Administrator",
                ["FUNC_MESSAGE_PM_TURN_FALSE"] = "The player has forbidden to send himself private messages",
                ["FUNC_MESSAGE_ALERT_TURN_FALSE"] = "The player has not been allowed to notify himself",

                ["FUNC_MESSAGE_NO_ARG_BROADCAST"] = "You can not send an empty broadcast message!",

                ["UI_ALERT_TITLE"] = "<size=14><b>Notification</b></size>",
		   		 		  						  	   		  	  			  	  			  	 				  	  	
                ["COMMAND_NOT_PERMISSION"] = "You dont have permissions to use this command",
                ["COMMAND_RENAME_NOTARG"] = "For rename use : /rename [NewNickname] [NewID (Optional)]",
                ["COMMAND_RENAME_NOT_ID"] = "Incorrect ID for renaming! Use Steam64ID or leave blank",
                ["COMMAND_RENAME_SUCCES"] = "You have successfully changed your nickname!\nyour nickname : {0}\nYour ID : {1}",
		   		 		  						  	   		  	  			  	  			  	 				  	  	
                ["COMMAND_PM_NOTARG"] = "To send pm use : /pm Nickname Message",
                ["COMMAND_PM_NOT_NULL_MSG"] = "Message is empty!",
                ["COMMAND_PM_NOT_USER"] = "User not found or offline",
                ["COMMAND_PM_SUCCESS"] = "Your private message sent successful\n\nMessage : {0}\n\nDelivered : {1}",
                ["COMMAND_PM_SEND_MSG"] = "Message from {0}\n\n{1}",

                ["COMMAND_R_NOTARG"] = "For reply use : /r Message",
                ["COMMAND_R_NOTMSG"] = "You dont have any private conversations yet!",

                ["FLOODERS_MESSAGE"] = "You're typing too fast! Please Wait {0} seconds",

                ["PREFIX_SETUP"] = "You have successfully removed the prefix {0}, it is already activated and installed",
                ["COLOR_CHAT_SETUP"] = "You have successfully picked up the <color={0}>chat color</color>, it is already activated and installed",
                ["COLOR_NICK_SETUP"] = "You have successfully taken the <color={0}>nickname color</color>, it is already activated and installed",

                ["PREFIX_RETURNRED"] = "Your prefix {0} expired, it was reset automatically",
                ["COLOR_CHAT_RETURNRED"] = "Action of your <color={0}>color chat</color> over, it is reset automatically",
                ["COLOR_NICK_RETURNRED"] = "Action of your <color={0}>color nick</color> over, it is reset automatically",

                ["WELCOME_PLAYER"] = "{0} came online",
                ["LEAVE_PLAYER"] = "{0} left",
                ["WELCOME_PLAYER_WORLD"] = "{0} came online. Country: {1}",
                ["LEAVE_PLAYER_REASON"] = "{0} left. Reason: {1}",

                ["IGNORE_ON_PLAYER"] = "You added {0} in black list",
                ["IGNORE_OFF_PLAYER"] = "You removed {0} from black list",
                ["IGNORE_NO_PM"] = "This player added you in black list. Your message has not been delivered.",
                ["IGNORE_NO_PM_ME"] = "You added this player in black list. Your message has not been delivered.",
                ["INGORE_NOTARG"] = "To ignore a player use : /ignore nickname",

                ["DISCORD_SEND_LOG_CHAT"] = "Player : {0}({1})\nFiltred message : {2}\nMessage : {3}",
                ["DISCORD_SEND_LOG_MUTE"] = "{0}({1}) give mute chat\nSuspect : {2}({3})\nReason : {4}",

                ["TITLE_FORMAT_DAYS"] = "D",
                ["TITLE_FORMAT_HOURSE"] = "H",
                ["TITLE_FORMAT_MINUTES"] = "M",
                ["TITLE_FORMAT_SECONDS"] = "S",

                ["IQCHAT_CONTEXT_TITLE"] = "SETTING UP A CHAT", ///"%TITLE%"
                ["IQCHAT_CONTEXT_SETTING_ELEMENT_TITLE"] = "CUSTOM SETTING", ///"%SETTING_ELEMENT%"
                ["IQCHAT_CONTEXT_INFORMATION_TITLE"] = "INFORMATION", ///"%INFORMATION%"
                ["IQCHAT_CONTEXT_SETTINGS_TITLE"] = "SETTINGS", ///"%SETTINGS%"
                ["IQCHAT_CONTEXT_SETTINGS_PM_TITLE"] = "Private messages", ///"%SETTINGS_PM%"
                ["IQCHAT_CONTEXT_SETTINGS_ALERT_TITLE"] = "Notification in the chat", ///"%SETTINGS_ALERT%"
                ["IQCHAT_CONTEXT_SETTINGS_ALERT_PM_TITLE"] = "Mention in the chat", ///"%SETTINGS_ALERT_PM%"
                ["IQCHAT_CONTEXT_SETTINGS_SOUNDS_TITLE"] = "Sound notification", ///"%SETTINGS_SOUNDS%"
                ["IQCHAT_CONTEXT_MUTE_STATUS_NOT"] = "NO", ///"%MUTE_STATUS_PLAYER%"
                ["IQCHAT_CONTEXT_MUTE_STATUS_TITLE"] = "Blocking the chat", ///"%MUTE_STATUS_TITLE%"
                ["IQCHAT_CONTEXT_IGNORED_STATUS_COUNT"] = "<size=11>{0}</size> human (а)", ///"%IGNORED_STATUS_COUNT%"
                ["IQCHAT_CONTEXT_IGNORED_STATUS_TITLE"] = "Ignoring", ///"%IGNORED_STATUS_TITLE%"
                ["IQCHAT_CONTEXT_NICK_DISPLAY_TITLE"] = "Your nickname", ///"%NICK_DISPLAY_TITLE%"
                ["IQCHAT_CONTEXT_NICK_DISPLAY_MESSAGE"] = "i love iqchat",
                ["IQCHAT_CONTEXT_SLIDER_PREFIX_TITLE"] = "Prefix", /// %SLIDER_PREFIX_TITLE%
                ["IQCHAT_CONTEXT_SLIDER_NICK_COLOR_TITLE"] = "Nick", /// %SLIDER_NICK_COLOR_TITLE%
                ["IQCHAT_CONTEXT_SLIDER_MESSAGE_COLOR_TITLE"] = "Message", /// %SLIDER_MESSAGE_COLOR_TITLE%
                ["IQCHAT_CONTEXT_SLIDER_IQRANK_TITLE"] = "Rank",
                ["IQCHAT_CONTEXT_SLIDER_IQRANK_TITLE_NULLER"] = "Absent",
                ["IQCHAT_CONTEXT_SLIDER_PREFIX_TITLE_DESCRIPTION"] = "Choosing a prefix", /// 
                ["IQCHAT_CONTEXT_SLIDER_CHAT_NICK_TITLE_DESCRIPTION"] = "Choosing a nickname color", /// 
                ["IQCHAT_CONTEXT_SLIDER_CHAT_MESSAGE_TITLE_DESCRIPTION"] = "Chat Color Selection", /// 
                ["IQCHAT_CONTEXT_SLIDER_IQRANK_TITLE_DESCRIPTION"] = "Rank Selection", /// 
                ["IQCHAT_CONTEXT_DESCRIPTION_PREFIX"] = "Prefix Setting",
                ["IQCHAT_CONTEXT_DESCRIPTION_NICK"] = "Setting up a nickname",
                ["IQCHAT_CONTEXT_DESCRIPTION_CHAT"] = "Setting up a message",
                ["IQCHAT_CONTEXT_DESCRIPTION_RANK"] = "Setting up the rank",

                ["IQCHAT_ALERT_TITLE"] = "ALERT", /// %TITLE_ALERT%

                ["IQCHAT_TITLE_IGNORE_AND_MUTE_MUTED"] = "LOCK MANAGEMENT",
                ["IQCHAT_TITLE_IGNORE_AND_MUTE_IGNORED"] = "IGNORING MANAGEMENT",
                ["IQCHAT_TITLE_IGNORE_TITLES"] = "<b>DO YOU REALLY WANT TO IGNORE\n{0}?</b>",
                ["IQCHAT_TITLE_IGNORE_TITLES_UNLOCK"] = "<b>DO YOU WANT TO REMOVE THE IGNORING FROM THE PLAYER\n{0}?</b>",
                ["IQCHAT_TITLE_IGNORE_BUTTON_YES"] = "<b>YES, I WANT TO</b>",
                ["IQCHAT_TITLE_IGNORE_BUTTON_NO"] = "<b>NO, I CHANGED MY MIND</b>",
                ["IQCHAT_TITLE_MODERATION_PANEL"] = "MODERATOR PANEL",

                ["IQCHAT_BUTTON_MODERATION_MUTE_MENU"] = "Lock Management",
                ["IQCHAT_BUTTON_MODERATION_MUTE_MENU_TITLE_ALERT"] = "SELECT AN ACTION",
                ["IQCHAT_BUTTON_MODERATION_MUTE_MENU_TITLE_ALERT_REASON"] = "SELECT THE REASON FOR BLOCKING",
                ["IQCHAT_BUTTON_MODERATION_MUTE_MENU_TITLE_ALERT_CHAT"] = "Block chat",
                ["IQCHAT_BUTTON_MODERATION_MUTE_MENU_TITLE_ALERT_VOICE"] = "Block voice",
                ["IQCHAT_BUTTON_MODERATION_UNMUTE_MENU_TITLE_ALERT_CHAT"] = "Unblock chat",
                ["IQCHAT_BUTTON_MODERATION_UNMUTE_MENU_TITLE_ALERT_VOICE"] = "Unlock voice",
                ["IQCHAT_BUTTON_MODERATION_MUTE_ALL_CHAT"] = "Block all chat",
                ["IQCHAT_BUTTON_MODERATION_UNMUTE_ALL_CHAT"] = "Unblock all chat",
                ["IQCHAT_BUTTON_MODERATION_MUTE_ALL_VOICE"] = "Block everyone's voice",
                ["IQCHAT_BUTTON_MODERATION_UNMUTE_ALL_VOICE"] = "Unlock everyone's voice",

                ["IQCHAT_FUNCED_NO_SEND_CHAT_MUTED"] = "You have an active chat lock : {0}",
                ["IQCHAT_FUNCED_NO_SEND_CHAT_MUTED_ALL_CHAT"] = "The administrator blocked everyone's chat. Expect full unblocking",
                ["IQCHAT_FUNCED_NO_SEND_CHAT_MUTED_ALL_VOICE"] = "The administrator blocked everyone's voice chat. Expect full unblocking",
                ["IQCHAT_FUNCED_NO_SEND_CHAT_UMMUTED_ALL_VOICE"] = "The administrator has unblocked the voice chat for everyone",
                ["IQCHAT_FUNCED_NO_SEND_CHAT_UNMUTED_ALL_CHAT"] = "The administrator has unblocked the chat for everyone",

                ["IQCHAT_FUNCED_ALERT_TITLE"] = "<color=#a7f64f><b>[MENTION]</b></color>",
                ["IQCHAT_FUNCED_ALERT_TITLE_ISMUTED"] = "The player has already been muted!",
                ["IQCHAT_FUNCED_ALERT_TITLE_SERVER"] = "Administrator",

                ["IQCHAT_INFO_ONLINE"] = "Now on the server :\n{0}",

                ["IQCHAT_INFO_ANTI_NOOB"] = "You first connected to the server!\nPlay some more {0}\nTo get access to send messages to the global and team chat!",
                ["IQCHAT_INFO_ANTI_NOOB_PM"] = "You first connected to the server!\nPlay some more {0}\nTo access sending messages to private messages!",

                ["XLEVELS_SYNTAX_PREFIX"] = "[{0} Level]",
                ["CLANS_SYNTAX_PREFIX"] = "[{0}]",
		   		 		  						  	   		  	  			  	  			  	 				  	  	
            }, this);
		   		 		  						  	   		  	  			  	  			  	 				  	  	
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["FUNC_MESSAGE_MUTE_CHAT"] = "{0} заблокировал чат игроку {1}\nДлительность : {2}\nПричина : {3}",
                ["FUNC_MESSAGE_UNMUTE_CHAT"] = "{0} разблокировал чат игроку {1}",
                ["FUNC_MESSAGE_MUTE_VOICE"] = "{0} заблокировал голос игроку {1}\nДлительность : {2}\nПричина : {3}",
                ["FUNC_MESSAGE_UNMUTE_VOICE"] = "{0} разблокировал голос игроку {1}",
                ["FUNC_MESSAGE_MUTE_ALL_CHAT"] = "Всем игрокам был заблокирован чат",
                ["FUNC_MESSAGE_UNMUTE_ALL_CHAT"] = "Всем игрокам был разблокирован чат",
                ["FUNC_MESSAGE_MUTE_ALL_VOICE"] = "Всем игрокам был заблокирован голос",
                ["FUNC_MESSAGE_MUTE_ALL_ALERT"] = "Блокировка Администратором",
                ["FUNC_MESSAGE_UNMUTE_ALL_VOICE"] = "Всем игрокам был разблокирован голос",

                ["FUNC_MESSAGE_PM_TURN_FALSE"] = "Игрок запретил присылать себе личные сообщения",
                ["FUNC_MESSAGE_ALERT_TURN_FALSE"] = "Игрок запретил уведомлять себя",

                ["FUNC_MESSAGE_NO_ARG_BROADCAST"] = "Вы не можете отправлять пустое сообщение в оповещение!",

                ["UI_ALERT_TITLE"] = "<size=14><b>Уведомление</b></size>",

                ["COMMAND_NOT_PERMISSION"] = "У вас недостаточно прав для данной команды",
                ["COMMAND_RENAME_NOTARG"] = "Используйте команду так : /rename [НовыйНик] [НовыйID (По желанию)]",
                ["COMMAND_RENAME_NOT_ID"] = "Неверно указан ID для переименования! Используйте Steam64ID, либо оставьте поле пустым",
                ["COMMAND_RENAME_SUCCES"] = "Вы успешно изменили ник!\nВаш ник : {0}\nВаш ID : {1}",

                ["COMMAND_PM_NOTARG"] = "Используйте команду так : /pm Ник Игрока Сообщение",
                ["COMMAND_PM_NOT_NULL_MSG"] = "Вы не можете отправлять пустое сообщение",
                ["COMMAND_PM_NOT_USER"] = "Игрок не найден или не в сети",
                ["COMMAND_PM_SUCCESS"] = "Ваше сообщение успешно доставлено\n\nСообщение : {0}\n\nДоставлено : {1}",
                ["COMMAND_PM_SEND_MSG"] = "Сообщение от {0}\n\n{1}",

                ["COMMAND_R_NOTARG"] = "Используйте команду так : /r Сообщение",
                ["COMMAND_R_NOTMSG"] = "Вам или вы ещё не писали игроку в личные сообщения!",

                ["FLOODERS_MESSAGE"] = "Вы пишите слишком быстро! Подождите {0} секунд",

                ["PREFIX_SETUP"] = "Вы успешно забрали префикс {0}, он уже активирован и установлен",
                ["COLOR_CHAT_SETUP"] = "Вы успешно забрали <color={0}>цвет чата</color>, он уже активирован и установлен",
                ["COLOR_NICK_SETUP"] = "Вы успешно забрали <color={0}>цвет ника</color>, он уже активирован и установлен",

                ["PREFIX_RETURNRED"] = "Действие вашего префикса {0} окончено, он сброшен автоматически",
                ["COLOR_CHAT_RETURNRED"] = "Действие вашего <color={0}>цвета чата</color> окончено, он сброшен автоматически",
                ["COLOR_NICK_RETURNRED"] = "Действие вашего <color={0}>цвет ника</color> окончено, он сброшен автоматически",

                ["WELCOME_PLAYER"] = "{0} зашел на сервер",
                ["LEAVE_PLAYER"] = "{0} вышел с сервера",
                ["WELCOME_PLAYER_WORLD"] = "{0} зашел на сервер.Из {1}",
                ["LEAVE_PLAYER_REASON"] = "{0} вышел с сервера.Причина {1}",

                ["IGNORE_ON_PLAYER"] = "Вы добавили игрока {0} в черный список",
                ["IGNORE_OFF_PLAYER"] = "Вы убрали игрока {0} из черного списка",
                ["IGNORE_NO_PM"] = "Данный игрок добавил вас в ЧС,ваше сообщение не будет доставлено",
                ["IGNORE_NO_PM_ME"] = "Вы добавили данного игрока в ЧС,ваше сообщение не будет доставлено",
                ["INGORE_NOTARG"] = "Используйте команду так : /ignore Ник Игрока",

                ["DISCORD_SEND_LOG_CHAT"] = "Игрок : {0}({1})\nФильтрованное сообщение : {2}\nИзначальное сообщение : {3}",
                ["DISCORD_SEND_LOG_MUTE"] = "{0}({1}) выдал блокировку чата\nИгрок : {2}({3})\nПричина : {4}",

                ["TITLE_FORMAT_DAYS"] = "Д",
                ["TITLE_FORMAT_HOURSE"] = "Ч",
                ["TITLE_FORMAT_MINUTES"] = "М",
                ["TITLE_FORMAT_SECONDS"] = "С",

                ["IQCHAT_CONTEXT_TITLE"] = "НАСТРОЙКА ЧАТА", ///"%TITLE%"
                ["IQCHAT_CONTEXT_SETTING_ELEMENT_TITLE"] = "ПОЛЬЗОВАТЕЛЬСКАЯ НАСТРОЙКА", ///"%SETTING_ELEMENT%"
                ["IQCHAT_CONTEXT_INFORMATION_TITLE"] = "ИНФОРМАЦИЯ", ///"%INFORMATION%"
                ["IQCHAT_CONTEXT_SETTINGS_TITLE"] = "НАСТРОЙКИ", ///"%SETTINGS%"
                ["IQCHAT_CONTEXT_SETTINGS_PM_TITLE"] = "Личные сообщения", ///"%SETTINGS_PM%"
                ["IQCHAT_CONTEXT_SETTINGS_ALERT_TITLE"] = "Оповещение в чате", ///"%SETTINGS_ALERT%"
                ["IQCHAT_CONTEXT_SETTINGS_ALERT_PM_TITLE"] = "Упоминание в чате", ///"%SETTINGS_ALERT_PM%"
                ["IQCHAT_CONTEXT_SETTINGS_SOUNDS_TITLE"] = "Звуковое оповещение", ///"%SETTINGS_SOUNDS%"
                ["IQCHAT_CONTEXT_MUTE_STATUS_NOT"] = "НЕТ", ///"%MUTE_STATUS_PLAYER%"
                ["IQCHAT_CONTEXT_MUTE_STATUS_TITLE"] = "Блокировка чата", ///"%MUTE_STATUS_TITLE%"
                ["IQCHAT_CONTEXT_IGNORED_STATUS_COUNT"] = "<size=11>{0}</size> человек (а)", ///"%IGNORED_STATUS_COUNT%"
                ["IQCHAT_CONTEXT_IGNORED_STATUS_TITLE"] = "Игнорирование", ///"%IGNORED_STATUS_TITLE%"
                ["IQCHAT_CONTEXT_NICK_DISPLAY_TITLE"] = "Ваш ник", ///"%NICK_DISPLAY_TITLE%"
                ["IQCHAT_CONTEXT_NICK_DISPLAY_MESSAGE"] = "люблю iqchat",
                ["IQCHAT_CONTEXT_SLIDER_PREFIX_TITLE"] = "Префикс", /// %SLIDER_PREFIX_TITLE%
                ["IQCHAT_CONTEXT_SLIDER_NICK_COLOR_TITLE"] = "Ник", /// %SLIDER_NICK_COLOR_TITLE%
                ["IQCHAT_CONTEXT_SLIDER_MESSAGE_COLOR_TITLE"] = "Чат", /// %SLIDER_MESSAGE_COLOR_TITLE%
                ["IQCHAT_CONTEXT_SLIDER_IQRANK_TITLE"] = "Ранг",
                ["IQCHAT_CONTEXT_SLIDER_IQRANK_TITLE_NULLER"] = "Отсутствует",
                ["IQCHAT_CONTEXT_SLIDER_PREFIX_TITLE_DESCRIPTION"] = "Выбор префикса", /// 
                ["IQCHAT_CONTEXT_SLIDER_CHAT_NICK_TITLE_DESCRIPTION"] = "Выбор цвета ника", /// 
                ["IQCHAT_CONTEXT_SLIDER_CHAT_MESSAGE_TITLE_DESCRIPTION"] = "Выбор цвета чата", /// 
                ["IQCHAT_CONTEXT_SLIDER_IQRANK_TITLE_DESCRIPTION"] = "Выбор ранга", /// 
                ["IQCHAT_CONTEXT_DESCRIPTION_PREFIX"] = "Настройка префикса",
                ["IQCHAT_CONTEXT_DESCRIPTION_NICK"] = "Настройка ника",
                ["IQCHAT_CONTEXT_DESCRIPTION_CHAT"] = "Настройка сообщения",
                ["IQCHAT_CONTEXT_DESCRIPTION_RANK"] = "Настройка ранга",

		   		 		  						  	   		  	  			  	  			  	 				  	  	
                ["IQCHAT_ALERT_TITLE"] = "УВЕДОМЛЕНИЕ", /// %TITLE_ALERT%
                ["IQCHAT_TITLE_IGNORE_AND_MUTE_MUTED"] = "УПРАВЛЕНИЕ БЛОКИРОВКАМИ",
                ["IQCHAT_TITLE_IGNORE_AND_MUTE_IGNORED"] = "УПРАВЛЕНИЕ ИГНОРИРОВАНИЕМ",
                ["IQCHAT_TITLE_IGNORE_TITLES"] = "<b>ВЫ ДЕЙСТВИТЕЛЬНО ХОТИТЕ ИГНОРИРОВАТЬ\n{0}?</b>",
                ["IQCHAT_TITLE_IGNORE_TITLES_UNLOCK"] = "<b>ВЫ ХОТИТЕ СНЯТЬ ИГНОРИРОВАНИЕ С ИГРОКА\n{0}?</b>",
                ["IQCHAT_TITLE_IGNORE_BUTTON_YES"] = "<b>ДА, ХОЧУ</b>",
                ["IQCHAT_TITLE_IGNORE_BUTTON_NO"] = "<b>НЕТ, ПЕРЕДУМАЛ</b>",
                ["IQCHAT_TITLE_MODERATION_PANEL"] = "ПАНЕЛЬ МОДЕРАТОРА",

                ["IQCHAT_BUTTON_MODERATION_MUTE_MENU"] = "Управление блокировками",
                ["IQCHAT_BUTTON_MODERATION_MUTE_MENU_TITLE_ALERT"] = "ВЫБЕРИТЕ ДЕЙСТВИЕ",
                ["IQCHAT_BUTTON_MODERATION_MUTE_MENU_TITLE_ALERT_REASON"] = "ВЫБЕРИТЕ ПРИЧИНУ БЛОКИРОВКИ",
                ["IQCHAT_BUTTON_MODERATION_MUTE_MENU_TITLE_ALERT_CHAT"] = "Заблокировать чат",
                ["IQCHAT_BUTTON_MODERATION_MUTE_MENU_TITLE_ALERT_VOICE"] = "Заблокировать голос",
                ["IQCHAT_BUTTON_MODERATION_UNMUTE_MENU_TITLE_ALERT_CHAT"] = "Разблокировать чат",
                ["IQCHAT_BUTTON_MODERATION_UNMUTE_MENU_TITLE_ALERT_VOICE"] = "Разблокировать голос",
                ["IQCHAT_BUTTON_MODERATION_MUTE_ALL_CHAT"] = "Заблокировать всем чат",
                ["IQCHAT_BUTTON_MODERATION_UNMUTE_ALL_CHAT"] = "Разблокировать всем чат",
                ["IQCHAT_BUTTON_MODERATION_MUTE_ALL_VOICE"] = "Заблокировать всем голос",
                ["IQCHAT_BUTTON_MODERATION_UNMUTE_ALL_VOICE"] = "Разблокировать всем голос",

                ["IQCHAT_FUNCED_NO_SEND_CHAT_MUTED"] = "У вас имеется активная блокировка чата : {0}",
                ["IQCHAT_FUNCED_NO_SEND_CHAT_MUTED_ALL_CHAT"] = "Администратор заблокировал всем чат. Ожидайте полной разблокировки",
                ["IQCHAT_FUNCED_NO_SEND_CHAT_MUTED_ALL_VOICE"] = "Администратор заблокировал всем голосоввой чат. Ожидайте полной разблокировки",
                ["IQCHAT_FUNCED_NO_SEND_CHAT_UMMUTED_ALL_VOICE"] = "Администратор разрблокировал всем голосоввой чат",
                ["IQCHAT_FUNCED_NO_SEND_CHAT_UNMUTED_ALL_CHAT"] = "Администратор разрблокировал всем чат",

                ["IQCHAT_FUNCED_ALERT_TITLE"] = "<color=#a7f64f><b>[УПОМИНАНИЕ]</b></color>",
                ["IQCHAT_FUNCED_ALERT_TITLE_ISMUTED"] = "Игрок уже был замучен!",
                ["IQCHAT_FUNCED_ALERT_TITLE_SERVER"] = "Администратор",

                ["IQCHAT_INFO_ONLINE"] = "Сейчас на сервере :\n{0}",
		   		 		  						  	   		  	  			  	  			  	 				  	  	
                ["IQCHAT_INFO_ANTI_NOOB"] = "Вы впервые подключились на сервер!\nОтыграйте еще {0}\nЧтобы получить доступ к отправке сообщений в глобальный и командный чат!",
                ["IQCHAT_INFO_ANTI_NOOB_PM"] = "Вы впервые подключились на сервер!\nОтыграйте еще {0}\nЧтобы получить доступ к отправке сообщений в личные сообщения!",
		   		 		  						  	   		  	  			  	  			  	 				  	  	
                ["XLEVELS_SYNTAX_PREFIX"] = "[{0} Level]",
                ["CLANS_SYNTAX_PREFIX"] = "[{0}]",

            }, this, "ru");

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["FUNC_MESSAGE_MUTE_CHAT"] = "{0} silenciado {1}\n Duración: {2}\nRazón: {3}",
                ["FUNC_MESSAGE_UNMUTE_CHAT"] = "{0} sin silenciar {1}",
                ["FUNC_MESSAGE_MUTE_VOICE"] = "{0} voz apagada a {1}\n Duracion : {2}\n Razon : {3}",
                ["FUNC_MESSAGE_UNMUTE_VOICE"] = "{0} voz no silenciada a {1}",
                ["FUNC_MESSAGE_MUTE_ALL_CHAT"] = "Chat desactivado",
                ["FUNC_MESSAGE_UNMUTE_ALL_CHAT"] = "Chat habilitado",
                ["FUNC_MESSAGE_MUTE_ALL_VOICE"] = "Chat de voz desactivado",
                ["FUNC_MESSAGE_UNMUTE_ALL_VOICE"] = "Chat de voz habilitado",
                ["FUNC_MESSAGE_MUTE_ALL_ALERT"] = "Bloqueo por parte del administrador",
                ["FUNC_MESSAGE_PM_TURN_FALSE"] = "El jugador tiene prohibido enviarse mensajes privados",
                ["FUNC_MESSAGE_ALERT_TURN_FALSE"] = "El jugador no ha podido notificarse a sí mismo",
                ["FUNC_MESSAGE_NO_ARG_BROADCAST"] = "No se puede enviar un mensaje vacío.",
                ["UI_ALERT_TITLE"] = "<size=14><b>Notificación</b></size>",
                ["COMMAND_NOT_PERMISSION"] = "No tienes permisos para usar este comando",
                ["COMMAND_RENAME_NOTARG"] = "Para renombrar utilice : /rename [NewNickname] [NewID (Optional)]",
                ["COMMAND_RENAME_NOT_ID"] = "¡ID incorrecto para renombrar! Utilice Steam64ID o déjelo en blanco",
                ["COMMAND_RENAME_SUCCES"] = "Has cambiado con éxito tu nombre de usuario. \n Tu nombre de usuario: {0}. \nTu ID: {1}.",
                ["COMMAND_PM_NOTARG"] = "Para enviar pm utilice : /pm [Nombre] [Mensaje]",
                ["COMMAND_PM_NOT_NULL_MSG"] = "¡El mensaje está vacío!",
                ["COMMAND_PM_NOT_USER"] = "Usuario no encontrado o desconectado",
                ["COMMAND_PM_SUCCESS"] = "Su mensaje privado enviado con éxito \n Mensage : {0}\n : Entregado{1}",
                ["COMMAND_PM_SEND_MSG"] = "Mensaje de {0}\n{1}",
                ["COMMAND_R_NOTARG"] = "Para responder utilice : /r Mensaje",
                ["COMMAND_R_NOTMSG"] = "Todavía no tienes ninguna conversación privada.",
                ["FLOODERS_MESSAGE"] = "¡Estás escribiendo demasiado rápido! Por favor, espere {0} segundos",
                ["PREFIX_SETUP"] = "Has eliminado con éxito el prefijo {0}.",
                ["COLOR_CHAT_SETUP"] = "Has obtenido un nuevo color en el chat",
                ["COLOR_NICK_SETUP"] = "Has cambiado tu nick correctamente del chat",
                ["PREFIX_RETURNRED"] = "Su prefijo {0} ha caducado, se ha restablecido automáticamente",
                ["COLOR_CHAT_RETURNRED"] = "Acción de su <color={0}>color de chat</color> más, se restablece automáticamente",
                ["COLOR_NICK_RETURNRED"] = "Acción de su <color={0}>color nick</color> sobre, se restablece automáticamente",
                ["WELCOME_PLAYER"] = "{0} Se ha conectado",
                ["LEAVE_PLAYER"] = "{0} izquierda",
                ["WELCOME_PLAYER_WORLD"] = "{0} Se ha conectado del Pais: {1}",
                ["LEAVE_PLAYER_REASON"] = "{0} Se ha desconectado. Razon: {1}",
                ["IGNORE_ON_PLAYER"] = "Has añadido {0} en la lista negra",
                ["IGNORE_OFF_PLAYER"] = "Has eliminado el jugador {0} de la lista negra",
                ["IGNORE_NO_PM"] = "Este jugador te ha añadido a la lista negra. Su mensaje no ha sido entregado.",
                ["IGNORE_NO_PM_ME"] = "Has añadido a este jugador en la lista negra. Su mensaje no ha sido entregado.",
                ["INGORE_NOTARG"] = "Para ignorar a un jugador utiliza : /ignore nickname",
                ["DISCORD_SEND_LOG_CHAT"] = "JUgador : {0}({1})\nMensaje filtrado : {2}\nMensages : {3}",
                ["DISCORD_SEND_LOG_MUTE"] = "{0}({1}) give mute chat\nSuspect : {2}({3})\nReason : {4}",
                ["TITLE_FORMAT_DAYS"] = "D",
                ["TITLE_FORMAT_HOURSE"] = "H",
                ["TITLE_FORMAT_MINUTES"] = "M",
                ["TITLE_FORMAT_SECONDS"] = "S",
                ["IQCHAT_CONTEXT_TITLE"] = "ESTABLECER UN CHAT",
                ["IQCHAT_CONTEXT_SETTING_ELEMENT_TITLE"] = "AJUSTE PERSONALIZADO",
                ["IQCHAT_CONTEXT_INFORMATION_TITLE"] = "INFORMACIÓN",
                ["IQCHAT_CONTEXT_SETTINGS_TITLE"] = "AJUSTES",
                ["IQCHAT_CONTEXT_SETTINGS_PM_TITLE"] = "Mensajes privados",
                ["IQCHAT_CONTEXT_SETTINGS_ALERT_TITLE"] = "Notificación en el chat",
                ["IQCHAT_CONTEXT_SETTINGS_ALERT_PM_TITLE"] = "Mención en el chat",
                ["IQCHAT_CONTEXT_SETTINGS_SOUNDS_TITLE"] = "Notificación sonora",
                ["IQCHAT_CONTEXT_MUTE_STATUS_NOT"] = "NO",
                ["IQCHAT_CONTEXT_MUTE_STATUS_TITLE"] = "Bloqueo del chat",
                ["IQCHAT_CONTEXT_IGNORED_STATUS_COUNT"] = "<size=11>{0}</size> humano (а)",
                ["IQCHAT_CONTEXT_IGNORED_STATUS_TITLE"] = "Ignorando",
                ["IQCHAT_CONTEXT_NICK_DISPLAY_TITLE"] = "Su apodo",
                ["IQCHAT_CONTEXT_NICK_DISPLAY_MESSAGE"] = "Me encanta Zoxiland",
                ["IQCHAT_CONTEXT_SLIDER_PREFIX_TITLE"] = "Prefijo",
                ["IQCHAT_CONTEXT_SLIDER_NICK_COLOR_TITLE"] = "Nick",
                ["IQCHAT_CONTEXT_SLIDER_MESSAGE_COLOR_TITLE"] = "Mensaje",
                ["IQCHAT_CONTEXT_SLIDER_IQRANK_TITLE"] = "Rango",
                ["IQCHAT_CONTEXT_SLIDER_IQRANK_TITLE_NULLER"] = "Ausente",
                ["IQCHAT_CONTEXT_SLIDER_PREFIX_TITLE_DESCRIPTION"] = "Elegir un prefijo",
                ["IQCHAT_CONTEXT_SLIDER_CHAT_NICK_TITLE_DESCRIPTION"] = "Elegir un color de apodo",
                ["IQCHAT_CONTEXT_SLIDER_CHAT_MESSAGE_TITLE_DESCRIPTION"] = "Selección del color del chat",
                ["IQCHAT_CONTEXT_SLIDER_IQRANK_TITLE_DESCRIPTION"] = "Selección de rangos",
                ["IQCHAT_CONTEXT_DESCRIPTION_PREFIX"] = "Ajuste del prefijo",
                ["IQCHAT_CONTEXT_DESCRIPTION_NICK"] = "Configurar un apodo",
                ["IQCHAT_CONTEXT_DESCRIPTION_CHAT"] = "Configurar un mensaje",
                ["IQCHAT_CONTEXT_DESCRIPTION_RANK"] = "Establecimiento del rango",
                ["IQCHAT_ALERT_TITLE"] = "ALERTA",
                ["IQCHAT_TITLE_IGNORE_AND_MUTE_MUTED"] = "GESTIÓN MUTEADOS",
                ["IQCHAT_TITLE_IGNORE_AND_MUTE_IGNORED"] = "GESTIÓN IGNORE",
                ["IQCHAT_TITLE_IGNORE_TITLES"] = "<b>¿REALMENTE QUIERES IGNORAR\n{0}?</b>",
                ["IQCHAT_TITLE_IGNORE_TITLES_UNLOCK"] = "<b>¿QUIERES QUITARLE AL JUGADOR LO DE IGNORAR?\n{0}?</b>",
                ["IQCHAT_TITLE_IGNORE_BUTTON_YES"] = "<b>SÍ, QUIERO</b>",
                ["IQCHAT_TITLE_IGNORE_BUTTON_NO"] = "<b>NO, HE CAMBIADO DE OPINIÓN</b>",
                ["IQCHAT_TITLE_MODERATION_PANEL"] = "PANEL DE MODERADORES",
                ["IQCHAT_BUTTON_MODERATION_MUTE_MENU"] = "Menu de muteados",
                ["IQCHAT_BUTTON_MODERATION_MUTE_MENU_TITLE_ALERT"] = "SELECCIONE UNA ACCIÓN",
                ["IQCHAT_BUTTON_MODERATION_MUTE_MENU_TITLE_ALERT_REASON"] = "SELECCIONE EL MOTIVO DEL BLOQUEO",
                ["IQCHAT_BUTTON_MODERATION_MUTE_MENU_TITLE_ALERT_CHAT"] = "Bloquear el Chat",
                ["IQCHAT_BUTTON_MODERATION_MUTE_MENU_TITLE_ALERT_VOICE"] = "Bloquear Voz",
                ["IQCHAT_BUTTON_MODERATION_UNMUTE_MENU_TITLE_ALERT_CHAT"] = "Desbloquear Chat",
                ["IQCHAT_BUTTON_MODERATION_UNMUTE_MENU_TITLE_ALERT_VOICE"] = "Desbloquear Voz",
                ["IQCHAT_BUTTON_MODERATION_MUTE_ALL_CHAT"] = "Bloquear todos los chats",
                ["IQCHAT_BUTTON_MODERATION_UNMUTE_ALL_CHAT"] = "Desbloquear todo el chat",
                ["IQCHAT_BUTTON_MODERATION_MUTE_ALL_VOICE"] = "Bloquear la voz de todos",
                ["IQCHAT_BUTTON_MODERATION_UNMUTE_ALL_VOICE"] = "Desbloquear la voz de todos",
                ["IQCHAT_FUNCED_NO_SEND_CHAT_MUTED"] = "Tienes un bloqueo de chat activo : {0}",
                ["IQCHAT_FUNCED_NO_SEND_CHAT_MUTED_ALL_CHAT"] = "El administrador ha bloqueado el chat. Espera el desbloqueo completo",
                ["IQCHAT_FUNCED_NO_SEND_CHAT_MUTED_ALL_VOICE"] = "El administrador ha bloqueado el chat de voz. Espera el desbloqueo completo",
                ["IQCHAT_FUNCED_NO_SEND_CHAT_UMMUTED_ALL_VOICE"] = "El administrador ha desbloqueado el chat de voz.",
                ["IQCHAT_FUNCED_NO_SEND_CHAT_UNMUTED_ALL_CHAT"] = "El administrador ha desbloqueado el chat",
                ["IQCHAT_FUNCED_ALERT_TITLE"] = "<color=#a7f64f><b>[MENCIÓN]</b></color>",
                ["IQCHAT_FUNCED_ALERT_TITLE_ISMUTED"] = "El jugador ya ha sido silenciado.",
                ["IQCHAT_FUNCED_ALERT_TITLE_SERVER"] = "Administrador",
                ["IQCHAT_INFO_ONLINE"] = "Now on the server :\n{0}",
                ["IQCHAT_INFO_ANTI_NOOB"] = "Tienes que jugar un poco mas para poder hablar por el chat {0}.",
                ["IQCHAT_INFO_ANTI_NOOB_PM"] = "No puedes enviar un privado por que es un jugador nuevo.",
                ["XLEVELS_SYNTAX_PREFIX"] = "[{0} Level]",
                ["CLANS_SYNTAX_PREFIX"] = "[{0}]",
		   		 		  						  	   		  	  			  	  			  	 				  	  	
            }, this, "es-ES");

            PrintWarning(LanguageEn ? "Language file uploaded successfully" : "Языковой файл загружен успешно");
        }
        private const String PermissionRename = "iqchat.renameuse";

        private void Request(string url, string payload, Action<int> callback = null)
        {
            Dictionary<string, string> header = new Dictionary<string, string>();
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
                                float seconds = float.Parse(Math.Ceiling((double)(int)json["retry_after"] / 1000).ToString());
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

            }, this, RequestMethod.POST, header);
        }
        private void DrawUI_IQChat_Context_AdminAndModeration(BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString, PermissionMute)) return;

            String InterfaceModeration = InterfaceBuilder.GetInterface("UI_Chat_Moderation");
            if (InterfaceModeration == null) return;

            InterfaceModeration = InterfaceModeration.Replace("%TITLE%", GetLang("IQCHAT_TITLE_MODERATION_PANEL", player.UserIDString));
            InterfaceModeration = InterfaceModeration.Replace("%COMMAND_MUTE_MENU%", $"newui.cmd action.mute.ignore open {SelectedAction.Mute}");
            InterfaceModeration = InterfaceModeration.Replace("%TEXT_MUTE_MENU%", GetLang("IQCHAT_BUTTON_MODERATION_MUTE_MENU", player.UserIDString));

            CuiHelper.AddUi(player, InterfaceModeration);

            DrawUI_IQChat_Update_MuteChat_All(player);
            DrawUI_IQChat_Update_MuteVoice_All(player);
        }

        void OnUserGroupRemoved(string id, string groupName)
        {
            String[] PermissionsGroup = permission.GetGroupPermissions(groupName);
            if (PermissionsGroup == null) return;

            foreach (String permName in PermissionsGroup)
                RemoveParametres(id, permName);
        }
        private void CheckValidateUsers()
        {
            Configuration.ControllerParameters Controller = config.ControllerParameter;
            Configuration.ControllerConnection ControllerConnection = config.ControllerConnect;

            List<Configuration.ControllerParameters.AdvancedFuncion> Prefixes = Controller.Prefixes.Prefixes;
            List<Configuration.ControllerParameters.AdvancedFuncion> NickColor = Controller.NickColorList;
            List<Configuration.ControllerParameters.AdvancedFuncion> ChatColor = Controller.MessageColorList;

            foreach (KeyValuePair<UInt64, User> Info in UserInformation)
            {
                if (Controller.Prefixes.TurnMultiPrefixes)
                {
                    foreach (String Prefix in Info.Value.Info.PrefixList.Where(prefixList => !Prefixes.Exists(i => i.Argument == prefixList)))
                        NextTick(() => Info.Value.Info.PrefixList.Remove(Prefix));
                }
                else
                {
                    if (!Prefixes.Exists(i => i.Argument == Info.Value.Info.Prefix))
                        Info.Value.Info.Prefix = ControllerConnection.SetupDefaults.PrefixDefault;
                }
                if (!NickColor.Exists(i => i.Argument == Info.Value.Info.ColorNick))
                    Info.Value.Info.ColorNick = ControllerConnection.SetupDefaults.NickDefault;

                if (!ChatColor.Exists(i => i.Argument == Info.Value.Info.ColorMessage))
                    Info.Value.Info.ColorMessage = ControllerConnection.SetupDefaults.MessageDefault;
            }
        }


        
                public class FancyMessage
        {
            public string content { get; set; }
            public bool tts { get; set; }
            public Embeds[] embeds { get; set; }

            public class Embeds
            {
                public string title { get; set; }
                public int color { get; set; }
                public List<Fields> fields { get; set; }
                public Footer footer { get; set; }
                public Authors author { get; set; }

                public Embeds(string title, int color, List<Fields> fields, Authors author, Footer footer)
                {
                    this.title = title;
                    this.color = color;
                    this.fields = fields;
                    this.author = author;
                    this.footer = footer;

                }
            }

            public FancyMessage(string content, bool tts, Embeds[] embeds)
            {
                this.content = content;
                this.tts = tts;
                this.embeds = embeds;
            }

            public string toJSON() => JsonConvert.SerializeObject(this);
        }
        public Boolean AddImage(String url, String shortname, UInt64 skin = 0) => (Boolean)ImageLibrary?.Call("AddImage", url, shortname, skin);
        void AlertUI(BasePlayer Sender, BasePlayer Recipient, string[] arg)
        {
            if (_interface == null)
            {
                PrintWarning(LanguageEn ? "We generate the interface, wait for a message about successful generation" : "Генерируем интерфейс, ожидайте сообщения об успешной генерации");
                return;
            }
            String Message = GetMessageInArgs(Sender, arg);
            if (Message == null) return;

            DrawUI_IQChat_Alert(Recipient, Message);
        }
        internal class AntiNoob
        {
            public DateTime DateConnection = DateTime.UtcNow;

            public Boolean IsNoob(Int32 TimeBlocked)
            {
                System.TimeSpan Time = DateTime.UtcNow.Subtract(DateConnection);
                return Time.TotalSeconds < TimeBlocked;
            }

            public Double LeftTime(Int32 TimeBlocked)
            {
                System.TimeSpan Time = DateTime.UtcNow.Subtract(DateConnection);

                return (TimeBlocked - Time.TotalSeconds);
            }
        }

        private void DrawUI_IQChat_Alert(BasePlayer player, String Description, String Title = null)
        {
            if (_interface == null)
            {
                PrintWarning("Генерируем интерфейс, ожидайте сообщения об успешной генерации");
                return;
            }
            String Interface = InterfaceBuilder.GetInterface("UI_Chat_Alert");
            if (Interface == null) return;

            Interface = Interface.Replace("%TITLE%", Title ?? GetLang("IQCHAT_ALERT_TITLE", player.UserIDString));
            Interface = Interface.Replace("%DESCRIPTION%", Description);

            CuiHelper.DestroyUi(player, InterfaceBuilder.UI_Chat_Alert);
            CuiHelper.AddUi(player, Interface);

            player.Invoke(() =>
            {
                CuiHelper.DestroyUi(player, InterfaceBuilder.UI_Chat_Alert);
            }, config.ControllerMessages.GeneralSetting.OtherSetting.TimeDeleteAlertUI);
        }
        void OnUserPermissionRevoked(string id, string permName) => RemoveParametres(id, permName);

        [ChatCommand("unmute")]
        void UnMuteCustomChat(BasePlayer Moderator, string cmd, string[] arg)
        {
            if (!permission.UserHasPermission(Moderator.UserIDString, PermissionMute)) return;
            if (arg == null || arg.Length != 1 || arg.Length > 1)
            {
                ReplySystem(Moderator, LanguageEn ? "Invalid syntax, please use : unmute Steam64ID" : "Неверный синтаксис,используйте : unmute Steam64ID");
                return;
            }
            string NameOrID = arg[0];
            BasePlayer target = GetPlayerNickOrID(NameOrID);
            if (target == null)
            {
                UInt64 Steam64ID = 0;
                if (UInt64.TryParse(NameOrID, out Steam64ID))
                {
                    if (UserInformation.ContainsKey(Steam64ID))
                    {
                        User Info = UserInformation[Steam64ID];
                        if (Info == null) return;
                        if (!Info.MuteInfo.IsMute(MuteType.Chat))
                        {
                            ReplySystem(Moderator, LanguageEn ? "The player does not have a chat lock" : "У игрока нет блокировки чата");
                            return;
                        }

                        Info.MuteInfo.UnMute(MuteType.Chat);
                        ReplySystem(Moderator, LanguageEn ? "You have unblocked the offline chat to the player" : "Вы разблокировали чат offline игроку");
                        return;
                    }
                    else
                    {
                        ReplySystem(Moderator, LanguageEn ? "This player is not on the server" : "Такого игрока нет на сервере");
                        return;
                    }
                }
                else
                {
                    ReplySystem(Moderator, LanguageEn ? "This player is not on the server" : "Такого игрока нет на сервере");
                    return;
                }
            }
            UnmutePlayer(target, MuteType.Chat, Moderator, false, true);
        }

        [ChatCommand("hunmute")]
        void HideUnMute(BasePlayer Moderator, string cmd, string[] arg)
        {
            if (!permission.UserHasPermission(Moderator.UserIDString, PermissionMute)) return;
            if (arg == null || arg.Length != 1 || arg.Length > 1)
            {
                ReplySystem(Moderator, LanguageEn ? "Invalid syntax, please use : hunmute Steam64ID/Nick" : "Неверный синтаксис,используйте : hunmute Steam64ID/Ник");
                return;
            }
            string NameOrID = arg[0];
            BasePlayer target = GetPlayerNickOrID(NameOrID);
            if (target == null)
            {
                UInt64 Steam64ID = 0;
                if (UInt64.TryParse(NameOrID, out Steam64ID))
                {
                    if (UserInformation.ContainsKey(Steam64ID))
                    {
                        User Info = UserInformation[Steam64ID];
                        if (Info == null) return;
                        if (!Info.MuteInfo.IsMute(MuteType.Chat))
                        {
                            ReplySystem(Moderator, LanguageEn ? "The player does not have a chat lock" : "У игрока нет блокировки чата");
                            return;
                        }

                        Info.MuteInfo.UnMute(MuteType.Chat);
                        ReplySystem(Moderator, LanguageEn ? "You have unblocked the offline chat to the player" : "Вы разблокировали чат offline игроку");
                        return;
                    }
                    else
                    {
                        ReplySystem(Moderator, LanguageEn ? "This player is not on the server" : "Такого игрока нет на сервере");
                        return;
                    }
                }
                else
                {
                    ReplySystem(Moderator, LanguageEn ? "This player is not on the server" : "Такого игрока нет на сервере");
                    return;
                }
            }

            UnmutePlayer(target, MuteType.Chat, Moderator, true, true);
        }
        private String XLevel_GetPrefix(BasePlayer player)
        {
            if (!XLevels || !config.ReferenceSetting.XLevelsSettings.UseXLevels) return String.Empty;
            return (String)XLevels?.CallHook("API_GetPlayerPrefix", player);
        }
        
                private void DrawUI_IQChat_Update_Check_Box(BasePlayer player, ElementsSettingsType Type, String OffsetMin, String OffsetMax, Boolean StatusCheckBox)
        {
            String Interface = InterfaceBuilder.GetInterface("UI_Chat_Context_CheckBox");
            User Info = UserInformation[player.userID];
            if (Info == null || Interface == null) return;

            String Name = $"{Type}";
            Interface = Interface.Replace("%NAME_CHECK_BOX%", Name);
            Interface = Interface.Replace("%COLOR%", !StatusCheckBox ? "0.4716981 0.4716981 0.4716981 1" : "0.6040971 0.4198113 1 1");
            Interface = Interface.Replace("%OFFSET_MIN%", OffsetMin);
            Interface = Interface.Replace("%OFFSET_MAX%", OffsetMax);
            Interface = Interface.Replace("%COMMAND_TURNED%", $"newui.cmd checkbox.controller {Type}");

            CuiHelper.DestroyUi(player, Name);
            CuiHelper.AddUi(player, Interface);
        }
        
        
        private void ControlledBadNick(IPlayer player)
        {
            if (player == null) return;
            Configuration.ControllerMessage ControllerMessage = config.ControllerMessages;

            String DisplayName = player.Name;

            Tuple<String, Boolean> GetTupleNick = BadWordsCleaner(DisplayName,
                ControllerMessage.Formatting.ControllerNickname.ReplaceBadNick,
                ControllerMessage.Formatting.ControllerNickname.BadNicks);
            DisplayName = GetTupleNick.Item1;

            DisplayName = RemoveLinkText(DisplayName);
            player.Rename(DisplayName);
        }

        
        
        private void SetupParametres(String ID, String Permissions)
        {
            UInt64 UserID = UInt64.Parse(ID);
            BasePlayer player = BasePlayer.FindByID(UserID);

            Configuration.ControllerConnection.Turned Controller = config.ControllerConnect.Turneds;
            Configuration.ControllerParameters Parameters = config.ControllerParameter;

            if (!UserInformation.ContainsKey(UserID)) return;
            User Info = UserInformation[UserID];

            if (Controller.TurnAutoSetupPrefix)
            {
                Configuration.ControllerParameters.AdvancedFuncion Prefixes = Parameters.Prefixes.Prefixes.FirstOrDefault(prefix => prefix.Permissions == Permissions);
                if (Prefixes == null) return;

                if (Parameters.Prefixes.TurnMultiPrefixes && !Info.Info.PrefixList.Contains(Prefixes.Argument))
                    Info.Info.PrefixList.Add(Prefixes.Argument);
                else Info.Info.Prefix = Prefixes.Argument;

                if (player != null)
                    ReplySystem(player, GetLang("PREFIX_SETUP", player.UserIDString, Prefixes.Argument));

                Log(LanguageEn ? $"Player ({UserID}) successfully retrieved the prefix {Prefixes.Argument}" : $"Игрок ({UserID}) успешно забрал префикс {Prefixes.Argument}");
            }
            if (Controller.TurnAutoSetupColorNick)
            {
                Configuration.ControllerParameters.AdvancedFuncion ColorNick = Parameters.NickColorList.FirstOrDefault(nick => nick.Permissions == Permissions);
                if (ColorNick == null) return;
                Info.Info.ColorNick = ColorNick.Argument;

                if (player != null)
                    ReplySystem(player, GetLang("COLOR_NICK_SETUP", player.UserIDString, ColorNick.Argument));

                Log(LanguageEn ? $"Player ({UserID}) successfully took the color of the nickname {ColorNick.Argument}" : $"Игрок ({UserID}) успешно забрал цвет ника {ColorNick.Argument}");
            }
            if (Controller.TurnAutoSetupColorChat)
            {
                Configuration.ControllerParameters.AdvancedFuncion ColorChat = Parameters.MessageColorList.FirstOrDefault(message => message.Permissions == Permissions);
                if (ColorChat == null) return;
                Info.Info.ColorMessage = ColorChat.Argument;

                if (player != null)
                    ReplySystem(player, GetLang("COLOR_CHAT_SETUP", player.UserIDString, ColorChat.Argument));

                Log(LanguageEn ? $"Player ({UserID}) successfully retrieved the color of the chat {ColorChat.Argument}" : $"Игрок ({UserID}) успешно забрал цвет чата {ColorChat.Argument}");
            }
        }
        [ChatCommand("saybro")]
        private void AlertOnlyPlayerChatCommand(BasePlayer Sender, String cmd, String[] args)
        {
            if (!permission.UserHasPermission(Sender.UserIDString, PermissionAlert)) return;
            if (args == null || args.Length == 0)
            {
                ReplySystem(Sender, LanguageEn ? "You didn't specify a player!" : "Вы не указали игрока!");
                return;
            }
            BasePlayer Recipient = BasePlayer.Find(args[0]);
            if (Recipient == null)
            {
                ReplySystem(Sender, LanguageEn ? "The player is not on the server" : "Игрока нет на сервере!");
                return;
            }
            Alert(Sender, Recipient, args.Skip(1).ToArray());
        }
        private void DrawUI_IQChat_Update_MuteChat_All(BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString, PermissionMutedAdmin)) return;

            String InterfaceAdministratorChat = InterfaceBuilder.GetInterface("UI_Chat_Administation_AllChat");
            if (InterfaceAdministratorChat == null) return;

            InterfaceAdministratorChat = InterfaceAdministratorChat.Replace("%TEXT_MUTE_ALLCHAT%", GetLang(!GeneralInfo.TurnMuteAllChat ? "IQCHAT_BUTTON_MODERATION_MUTE_ALL_CHAT" : "IQCHAT_BUTTON_MODERATION_UNMUTE_ALL_CHAT", player.UserIDString));
            InterfaceAdministratorChat = InterfaceAdministratorChat.Replace("%COMMAND_MUTE_ALLCHAT%", $"newui.cmd action.mute.ignore mute.controller {SelectedAction.Mute} mute.all.chat");

            CuiHelper.DestroyUi(player, "ModeratorMuteAllChat");
            CuiHelper.AddUi(player, InterfaceAdministratorChat);
        }

        
        
        [ConsoleCommand("mute")]
        void MuteCustomAdmin(ConsoleSystem.Arg arg)
        {
            if (arg.Player() != null)
                if (!permission.UserHasPermission(arg.Player().UserIDString, PermissionMute)) return;
            if (arg == null || arg.Args == null || arg.Args.Length != 3 || arg.Args.Length > 3)
            {
                PrintWarning(LanguageEn ? "Invalid syntax, use : mute Steam64ID/Nick Reason Time(seconds)" : "Неверный синтаксис,используйте : mute Steam64ID/Ник Причина Время(секунды)");
                return;
            }
            string NameOrID = arg.Args[0];
            string Reason = arg.Args[1];
            Int32 TimeMute = 0;
            if (!Int32.TryParse(arg.Args[2], out TimeMute))
            {
                PrintWarning(LanguageEn ? "Enter time in numbers!" : "Введите время цифрами!");
                return;
            }
            BasePlayer target = GetPlayerNickOrID(NameOrID);
            if (target == null)
            {
                UInt64 Steam64ID = 0;
                if (UInt64.TryParse(NameOrID, out Steam64ID))
                {
                    if (UserInformation.ContainsKey(Steam64ID))
                    {
                        User Info = UserInformation[Steam64ID];
                        if (Info == null) return;
                        if (Info.MuteInfo.IsMute(MuteType.Chat))
                        {
                            PrintWarning(LanguageEn ? "The player already has a chat lock" : "Игрок уже имеет блокировку чата");
                            return;
                        }

                        Info.MuteInfo.SetMute(MuteType.Chat, TimeMute);
                        PrintWarning(LanguageEn ? "Chat blocking issued to offline player" : "Блокировка чата выдана offline-игроку");
                        return;
                    }
                    else
                    {
                        PrintWarning(LanguageEn ? "This player is not on the server" : "Такого игрока нет на сервере");
                        return;
                    }
                }
                else
                {
                    PrintWarning(LanguageEn ? "This player is not on the server" : "Такого игрока нет на сервере");
                    return;
                }
            }

            MutePlayer(target, MuteType.Chat, 0, arg.Player(), Reason, TimeMute, false, true);
            Puts(LanguageEn ? "Successfully" : "Успешно");
        }

        [ConsoleCommand("rename")]
        private void ConsoleCommandRename(ConsoleSystem.Arg args)
        {
            BasePlayer Renamer = args.Player();
            if (Renamer == null)
            {
                PrintWarning(LanguageEn ? "You can only use this command while on the server" : "Вы можете использовать эту команду только находясь на сервере");
                return;
            }

            if (!permission.UserHasPermission(Renamer.UserIDString, PermissionRename)) return;
            GeneralInformation General = GeneralInfo;
            if (General == null) return;

            if (args.Args.Length == 0 || args == null)
            {
                ReplySystem(Renamer, lang.GetMessage("COMMAND_RENAME_NOTARG", this, Renamer.UserIDString));
                return;
            }

            String Name = args.Args[0];
            UInt64 ID = Renamer.userID;
            if (args.Args.Length == 2 && args.Args[1] != null && !String.IsNullOrWhiteSpace(args.Args[1]))
                if (!UInt64.TryParse(args.Args[1], out ID))
                {
                    ReplySystem(Renamer, lang.GetMessage("COMMAND_RENAME_NOT_ID", this, Renamer.UserIDString));
                    return;
                }

            if (General.RenameList.ContainsKey(Renamer.userID))
            {
                General.RenameList[Renamer.userID].RenameNick = Name;
                General.RenameList[Renamer.userID].RenameID = ID;
            }
            else General.RenameList.Add(Renamer.userID, new GeneralInformation.RenameInfo { RenameNick = Name, RenameID = ID });

            ReplySystem(Renamer, GetLang("COMMAND_RENAME_SUCCES", Renamer.UserIDString, Name, ID));
            Renamer.displayName = Name;
        }
        void API_ALERT_PLAYER_UI(BasePlayer player, String Message) => DrawUI_IQChat_Alert(player, Message);
        private enum SelectedParametres
        {
            DropList,
            Slider
        }
        private const String PermissionHideDisconnection = "iqchat.hidedisconnection";

        [ChatCommand("mute")]
        void MuteCustomChat(BasePlayer Moderator, string cmd, string[] arg)
        {
            if (!permission.UserHasPermission(Moderator.UserIDString, PermissionMute)) return;
            if (arg == null || arg.Length != 3 || arg.Length > 3)
            {
                ReplySystem(Moderator, LanguageEn ? "Invalid syntax, use : mute Steam64ID/Nick Reason Time(seconds)" : "Неверный синтаксис, используйте : mute Steam64ID/Ник Причина Время(секунды)");
                return;
            }
            string NameOrID = arg[0];
            string Reason = arg[1];
            Int32 TimeMute = 0;
            if (!Int32.TryParse(arg[2], out TimeMute))
            {
                ReplySystem(Moderator, LanguageEn ? "Enter time in numbers!" : "Введите время цифрами!");
                return;
            }
            BasePlayer target = GetPlayerNickOrID(NameOrID);
            if (target == null)
            {
                UInt64 Steam64ID = 0;
                if (UInt64.TryParse(NameOrID, out Steam64ID))
                {
                    if (UserInformation.ContainsKey(Steam64ID))
                    {
                        User Info = UserInformation[Steam64ID];
                        if (Info == null) return;
                        if (Info.MuteInfo.IsMute(MuteType.Chat))
                        {
                            ReplySystem(Moderator, LanguageEn ? "The player already has a chat lock" : "Игрок уже имеет блокировку чата");
                            return;
                        }

                        Info.MuteInfo.SetMute(MuteType.Chat, TimeMute);
                        ReplySystem(Moderator, LanguageEn ? "Chat blocking issued to offline player" : "Блокировка чата выдана offline-игроку");
                        return;
                    }
                    else
                    {
                        ReplySystem(Moderator, LanguageEn ? "This player is not on the server" : "Такого игрока нет на сервере");
                        return;
                    }
                }
                else
                {
                    ReplySystem(Moderator, LanguageEn ? "This player is not on the server" : "Такого игрока нет на сервере");
                    return;
                }
            }

            MutePlayer(target, MuteType.Chat, 0, Moderator, Reason, TimeMute, false, true);
        }
        
        
        
        private void DiscordLoggCommand(BasePlayer player, String Command, String[] Args)
        {
            Configuration.OtherSettings.General Commands = config.OtherSetting.LogsChatCommands;
            if (!Commands.UseLogged) return;

            List<Fields> fields = new List<Fields>
                        {
                            new Fields(LanguageEn ? "Nick" : "Ник", player.displayName, true),
                            new Fields("Steam64ID", player.UserIDString, true),
                            new Fields(LanguageEn ? "Command" : "Команда", $"/{Command} ", true),
                        };

            String Arguments = String.Join(" ", Args);
            if (Args != null && Arguments != null && Arguments.Length != 0 && !String.IsNullOrWhiteSpace(Arguments))
                fields.Insert(fields.Count, new Fields(LanguageEn ? "Arguments" : "Аргументы", Arguments, false));

            FancyMessage newMessage = new FancyMessage(null, false, new FancyMessage.Embeds[1] { new FancyMessage.Embeds(null, 10710525, fields, new Authors("IQChat Command-History", null, "https://i.imgur.com/xiwsg5m.png", null), null) });

            Request($"{Commands.Webhooks}", newMessage.toJSON());
        }
        [ConsoleCommand("alertuip")]
        private void AlertUIPConsoleCommand(ConsoleSystem.Arg args)
        {
            BasePlayer Sender = args.Player();
            if (Sender != null)
                if (!permission.UserHasPermission(Sender.UserIDString, PermissionAlert)) return;
            if (args.Args == null || args.Args.Length == 0)
            {
                if (Sender != null)
                    ReplySystem(Sender, LanguageEn ? "You didn't specify a player!" : "Вы не указали игрока!");
                else PrintWarning(LanguageEn ? "You didn't specify a player!" : "Вы не указали игрока!");
                return;
            }
            BasePlayer Recipient = BasePlayer.Find(args.Args[0]);
            if (Recipient == null)
            {
                if (Sender != null)
                    ReplySystem(Sender, LanguageEn ? "The player is not on the server!" : "Игрока нет на сервере!");
                else PrintWarning(LanguageEn ? "The player is not on the server!" : "Игрока нет на сервере!");
                return;
            }
            AlertUI(Sender, Recipient, args.Args.Skip(1).ToArray());
        }
        public void AnwserMessage(BasePlayer player, String Message)
        {
            Configuration.AnswerMessage Anwser = config.AnswerMessages;
            if (!Anwser.UseAnswer) return;
            foreach (KeyValuePair<String, Configuration.LanguageController> Anwsers in Anwser.AnswerMessageList)
                if (Message.Contains(Anwsers.Key.ToLower()))
                    ReplySystem(player, GetMessages(player, Anwsers.Value.LanguageMessages));
        }

        void OnGroupPermissionRevoked(string name, string perm)
        {
            String[] PlayerGroups = permission.GetUsersInGroup(name);
            if (PlayerGroups == null) return;

            foreach (String playerInfo in PlayerGroups)
            {
                BasePlayer player = BasePlayer.FindByID(UInt64.Parse(playerInfo.Substring(0, 17)));
                if (player == null) return;

                RemoveParametres(player.UserIDString, perm);
            }
        }
        public static StringBuilder sb = new StringBuilder();
        void Init()
        {
            ReadData();
        }
        String IQRankGetNameRankKey(string Key) => (string)(IQRankSystem?.Call("API_GET_RANK_NAME", Key));

        
                private void DrawUI_IQChat_Sliders(BasePlayer player, String Name, String OffsetMin, String OffsetMax, TakeElementUser ElementType)
        {
            String Interface = InterfaceBuilder.GetInterface("UI_Chat_Slider");
            if (Interface == null) return;

            Interface = Interface.Replace("%OFFSET_MIN%", OffsetMin);
            Interface = Interface.Replace("%OFFSET_MAX%", OffsetMax);
            Interface = Interface.Replace("%NAME%", Name);
            Interface = Interface.Replace("%COMMAND_LEFT_SLIDE%", $"newui.cmd slider.controller {ElementType} -");
            Interface = Interface.Replace("%COMMAND_RIGHT_SLIDE%", $"newui.cmd slider.controller {ElementType} +");

            CuiHelper.DestroyUi(player, Name);
            CuiHelper.AddUi(player, Interface);

            DrawUI_IQChat_Slider_Update_Argument(player, ElementType);
        }
        String API_GET_DEFAULT_NICK_COLOR() => config.ControllerConnect.SetupDefaults.NickDefault;

        void OnUserGroupAdded(string id, string groupName)
        {
            String[] PermissionsGroup = permission.GetGroupPermissions(groupName);
            if (PermissionsGroup == null) return;
            foreach (String permName in PermissionsGroup)
                SetupParametres(id, permName);
        }
        private const String PermissionHideConnection = "iqchat.hideconnection";

        void ReplyBroadcast(String Message, String CustomPrefix = null, String CustomAvatar = null, Boolean AdminAlert = false)
        {
            foreach (BasePlayer p in !AdminAlert ? BasePlayer.activePlayerList.Where(p => UserInformation[p.userID].Settings.TurnBroadcast) : BasePlayer.activePlayerList)
                ReplySystem(p, Message, CustomPrefix, CustomAvatar);
        }
        
        
        [ConsoleCommand("alert")]
        private void AlertConsoleCommand(ConsoleSystem.Arg args)
        {
            BasePlayer Sender = args.Player();
            if (Sender != null)
                if (!permission.UserHasPermission(Sender.UserIDString, PermissionAlert)) return;

            Alert(Sender, args.Args, false);
        }
        private enum TakeElementUser
        {
            Prefix,
            Nick,
            Chat,
            Rank,
            MultiPrefix
        }
        
        private static ConfigurationOld configOld = new ConfigurationOld();
        public class User
        {
            public Information Info = new Information();
            public Setting Settings = new Setting();
            public Mute MuteInfo = new Mute();
            internal class Information
            {
                public String Prefix;
                public String ColorNick;
                public String ColorMessage;
                public String Rank;

                public List<String> PrefixList = new List<String>();
            }

            internal class Setting
            {
                public Boolean TurnPM = true;
                public Boolean TurnAlert = true;
                public Boolean TurnBroadcast = true;
                public Boolean TurnSound = true;

                public List<UInt64> IgnoreUsers = new List<UInt64>();

                public Boolean IsIgnored(UInt64 TargetID) => IgnoreUsers.Contains(TargetID);
                public void IgnoredAddOrRemove(UInt64 TargetID)
                {
                    if (IsIgnored(TargetID))
                        IgnoreUsers.Remove(TargetID);
                    else IgnoreUsers.Add(TargetID);
                }
            }

            internal class Mute
            {
                public Double TimeMuteChat;
                public Double TimeMuteVoice;

                public Double GetTime(MuteType Type)
                {
                    Double TimeMuted = 0;
                    switch (Type)
                    {
                        case MuteType.Chat:
                            TimeMuted = TimeMuteChat - CurrentTime;
                            break;
                        case MuteType.Voice:
                            TimeMuted = TimeMuteVoice - CurrentTime;
                            break;
                        default:
                            break;
                    }
                    return TimeMuted;
                }
                public void SetMute(MuteType Type, Int32 Time)
                {
                    switch (Type)
                    {
                        case MuteType.Chat:
                            TimeMuteChat = Time + CurrentTime;
                            break;
                        case MuteType.Voice:
                            TimeMuteVoice = Time + CurrentTime;
                            break;
                        default:
                            break;
                    }
                }
                public void UnMute(MuteType Type)
                {
                    switch (Type)
                    {
                        case MuteType.Chat:
                            TimeMuteChat = 0;
                            break;
                        case MuteType.Voice:
                            TimeMuteVoice = 0;
                            break;
                        default:
                            break;
                    }
                }
                public Boolean IsMute(MuteType Type) => GetTime(Type) > 0;
            }
        }
        public enum MuteType
        {
            Chat,
            Voice
        }
        [ChatCommand("adminalert")]
        private void AdminAlertChatCommand(BasePlayer Sender, String cmd, String[] args)
        {
            if (!permission.UserHasPermission(Sender.UserIDString, PermissionAlert)) return;
            Alert(Sender, args, true);
        }


        
        
        
        private class ImageUi
        {
            private static Coroutine coroutineImg = null;
            private static Dictionary<string, string> Images = new Dictionary<string, string>();
            public static void DownloadImages() { coroutineImg = ServerMgr.Instance.StartCoroutine(AddImage()); }

            private static IEnumerator AddImage()
            {
                _.PrintWarning(LanguageEn ? "Generating interface, wait ~10-15 seconds!" : "Генерируем интерфейс, ожидайте ~10-15 секунд!");

                foreach (String Key in _.KeyImages)
                {
                    string uri = $"https://iqsystem.skyplugins.ru/iqchat/getimageui/{Key}/WIwsqNNWF7nN";
                    UnityWebRequest www = UnityWebRequestTexture.GetTexture(uri);
                    yield return www.SendWebRequest();

                    if (_ == null)
                        yield break;
                    if (www.isNetworkError || www.isHttpError)
                    {
                        _.PrintWarning(string.Format("Image download error! Error: {0}, Image name: {1}", www.error, Key));
                        www.Dispose();
                        coroutineImg = null;
                        yield break;
                    }
                    Texture2D texture = DownloadHandlerTexture.GetContent(www);
                    if (texture != null)
                    {
                        byte[] bytes = texture.EncodeToPNG();

                        var image = FileStorage.server.Store(bytes, FileStorage.Type.png, CommunityEntity.ServerInstance.net.ID).ToString();
                        if (!Images.ContainsKey(Key))
                            Images.Add(Key, image);
                        else
                            Images[Key] = image;
                        UnityEngine.Object.DestroyImmediate(texture);
                    }

                    www.Dispose();
                    yield return CoroutineEx.waitForSeconds(0.02f);
                }
                coroutineImg = null;

                _interface = new InterfaceBuilder();
                _.PrintWarning(LanguageEn ? "Interface loaded successfully!" : "Интерфейс успешно загружен!");
            }

            public static string GetImage(String ImgKey)
            {
                if (Images.ContainsKey(ImgKey))
                    return Images[ImgKey];
                return _.GetImage("LOADING");
            }

            public static void Unload()
            {
                coroutineImg = null;
                foreach (var item in Images)
                    FileStorage.server.RemoveExact(uint.Parse(item.Value), FileStorage.Type.png, CommunityEntity.ServerInstance.net.ID, 0U);
            }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                try
                {
                    configOld = Config.ReadObject<ConfigurationOld>();
                    if (configOld != null)
                    {
                        string file =
                            $"{Interface.Oxide.ConfigDirectory}{Path.DirectorySeparatorChar}{Name}.backup_old_system.{DateTime.Now:yyyy-MM-dd hh-mm-ss}.json";
                        Config.WriteObject(configOld, false, file);
                        PrintWarning($"A BACKUP OF THE OLD CONFIGURATION WAS CREATED - {file}");
                    }
                }
                catch { }

                config = Config.ReadObject<Configuration>();
                if (config == null) LoadDefaultConfig();

                if (config.ControllerMessages.Formatting.ControllerNickname.AllowedLinkNick == null ||
                    config.ControllerMessages.Formatting.ControllerNickname.AllowedLinkNick.Count == 0)
                    config.ControllerMessages.Formatting.ControllerNickname.AllowedLinkNick = new List<String>()
                    {
                        "mysite.com"
                    };
            }
            catch
            {
                PrintWarning(LanguageEn
                    ? $"Error #132 read configuration 'oxide/config/{Name}', create a new configuration!!"
                    : $"Ошибка #132 чтения конфигурации 'oxide/config/{Name}', создаём новую конфигурацию!!");

                LoadDefaultConfig();
            }

            NextTick(SaveConfig);
        }
        public Dictionary<UInt64, User> UserInformation = new Dictionary<UInt64, User>();
        Boolean API_IS_IGNORED(UInt64 UserHas, UInt64 User)
        {
            if (!UserInformation.ContainsKey(UserHas)) return false;
            if (!UserInformation.ContainsKey(User)) return false;

            return UserInformation[UserHas].Settings.IsIgnored(User);
        }
        
                String IQRankGetRank(ulong userID) => (string)(IQRankSystem?.Call("API_GET_RANK_NAME", userID));

        void OnPlayerDisconnected(BasePlayer player, string reason) => AlertDisconnected(player, reason);
        private class ConfigurationOld
        {
                        [JsonProperty(LanguageEn ? "Setting up player information" : "Настройка информации о игроке")]
            public ControllerConnection ControllerConnect = new ControllerConnection();
            internal class ControllerConnection
            {
                [JsonProperty(LanguageEn ? "Function switches" : "Перключатели функций")]
                public Turned Turneds = new Turned();
                [JsonProperty(LanguageEn ? "Setting Standard Values" : "Настройка стандартных значений")]
                public SetupDefault SetupDefaults = new SetupDefault();

                internal class SetupDefault
                {
                    [JsonProperty(LanguageEn ? "This prefix will be set if the player entered the server for the first time or in case of expiration of the rights to the prefix that he had earlier" : "Данный префикс установится если игрок впервые зашел на сервер или в случае окончания прав на префикс, который у него стоял ранее")]
                    public String PrefixDefault = "<color=#CC99FF>[ИГРОК]</color>";
                    [JsonProperty(LanguageEn ? "This nickname color will be set if the player entered the server for the first time or in case of expiration of the rights to the nickname color that he had earlier" : "Данный цвет ника установится если игрок впервые зашел на сервер или в случае окончания прав на цвет ника, который у него стоял ранее")]
                    public String NickDefault = "#33CCCC";
                    [JsonProperty(LanguageEn ? "This chat color will be set if the player entered the server for the first time or in case of expiration of the rights to the chat color that he had earlier" : "Данный цвет чата установится если игрок впервые зашел на сервер или в случае окончания прав на цвет чата, который у него стоял ранее")]
                    public String MessageDefault = "#0099FF";
                }
                internal class Turned
                {
                    [JsonProperty(LanguageEn ? "Set automatically a prefix to a player when he got the rights to it" : "Устанавливать автоматически префикс игроку, когда он получил права на него")]
                    public Boolean TurnAutoSetupPrefix;
                    [JsonProperty(LanguageEn ? "Set automatically the color of the nickname to the player when he got the rights to it" : "Устанавливать автоматически цвет ника игроку, когда он получил права на него")]
                    public Boolean TurnAutoSetupColorNick;
                    [JsonProperty(LanguageEn ? "Set the chat color automatically to the player when he got the rights to it" : "Устанавливать автоматически цвет чата игроку, когда он получил права на него")]
                    public Boolean TurnAutoSetupColorChat;
                    [JsonProperty(LanguageEn ? "Automatically reset the prefix when the player's rights to it expire" : "Сбрасывать автоматически префикс при окончании прав на него у игрока")]
                    public Boolean TurnAutoDropPrefix;
                    [JsonProperty(LanguageEn ? "Automatically reset the color of the nickname when the player's rights to it expire" : "Сбрасывать автоматически цвет ника при окончании прав на него у игрока")]
                    public Boolean TurnAutoDropColorNick;
                    [JsonProperty(LanguageEn ? "Automatically reset the color of the chat when the rights to it from the player expire" : "Сбрасывать автоматически цвет чата при окончании прав на него у игрока")]
                    public Boolean TurnAutoDropColorChat;
                }
            }
            
                        [JsonProperty(LanguageEn ? "Setting options for the player" : "Настройка параметров для игрока")]
            public ControllerParameters ControllerParameter = new ControllerParameters();
            internal class ControllerParameters
            {
                [JsonProperty(LanguageEn ? "Setting the display of options for player selection" : "Настройка отображения параметров для выбора игрока")]
                public VisualSettingParametres VisualParametres = new VisualSettingParametres();
                [JsonProperty(LanguageEn ? "List and customization of colors for a nickname" : "Список и настройка цветов для ника")]
                public List<AdvancedFuncion> NickColorList = new List<AdvancedFuncion>();
                [JsonProperty(LanguageEn ? "List and customize colors for chat messages" : "Список и настройка цветов для сообщений в чате")]
                public List<AdvancedFuncion> MessageColorList = new List<AdvancedFuncion>();
                [JsonProperty(LanguageEn ? "List and configuration of prefixes in chat" : "Список и настройка префиксов в чате")]
                public PrefixSetting Prefixes = new PrefixSetting();
                internal class PrefixSetting
                {
                    [JsonProperty(LanguageEn ? "Enable support for multiple prefixes at once (true - multiple prefixes can be set/false - only 1 can be set to choose from)" : "Включить поддержку нескольких префиксов сразу (true - можно установить несколько префиксов/false - установить можно только 1 на выбор)")]
                    public Boolean TurnMultiPrefixes;
                    [JsonProperty(LanguageEn ? "The maximum number of prefixes that can be set at a time (This option only works if setting multiple prefixes is enabled)" : "Максимальное количество префиксов, которое можно установить за раз(Данный параметр работает только если включена установка нескольких префиксов)")]
                    public Int32 MaximumMultiPrefixCount;
                    [JsonProperty(LanguageEn ? "List of prefixes and their settings" : "Список префиксов и их настройка")]
                    public List<AdvancedFuncion> Prefixes = new List<AdvancedFuncion>();
                }
		   		 		  						  	   		  	  			  	  			  	 				  	  	
                internal class AdvancedFuncion
                {
                    [JsonProperty(LanguageEn ? "Permission" : "Права")]
                    public String Permissions;
                    [JsonProperty(LanguageEn ? "Argument" : "Значение")]
                    public String Argument;
                }

                internal class VisualSettingParametres
                {
                    [JsonProperty(LanguageEn ? "Player prefix selection display type - (0 - dropdown list, 1 - slider (Please note that if you have multi-prefix enabled, the dropdown list will be set))" : "Тип отображения выбора префикса для игрока - (0 - выпадающий список, 1 - слайдер (Учтите, что если у вас включен мульти-префикс, будет установлен выпадающий список))")]
                    public SelectedParametres PrefixType;
                    [JsonProperty(LanguageEn ? "Display type of player's nickname color selection - (0 - drop-down list, 1 - slider)" : "Тип отображения выбора цвета ника для игрока - (0 - выпадающий список, 1 - слайдер)")]
                    public SelectedParametres NickColorType;
                    [JsonProperty(LanguageEn ? "Display type of message color choice for the player - (0 - drop-down list, 1 - slider)" : "Тип отображения выбора цвета сообщения для игрока - (0 - выпадающий список, 1 - слайдер)")]
                    public SelectedParametres ChatColorType;
                    [JsonProperty(LanguageEn ? "IQRankSystem : Player rank selection display type - (0 - drop-down list, 1 - slider)" : "IQRankSystem : Тип отображения выбора ранга для игрока - (0 - выпадающий список, 1 - слайдер)")]
                    public SelectedParametres IQRankSystemType;
                }
            }
            
                        [JsonProperty(LanguageEn ? "Plugin mute settings" : "Настройка мута в плагине")]
            public ControllerMute ControllerMutes = new ControllerMute();
            internal class ControllerMute
            {
                [JsonProperty(LanguageEn ? "Setting up automatic muting" : "Настройка автоматического мута")]
                public AutoMute AutoMuteSettings = new AutoMute();
                internal class AutoMute
                {
                    [JsonProperty(LanguageEn ? "Enable automatic muting for forbidden words (true - yes/false - no)" : "Включить автоматический мут по запрещенным словам(true - да/false - нет)")]
                    public Boolean UseAutoMute;
                    [JsonProperty(LanguageEn ? "Reason for automatic muting" : "Причина автоматического мута")]
                    public Muted AutoMuted;
                }
                [JsonProperty(LanguageEn ? "Additional setting for logging about mutes in discord" : "Дополнительная настройка для логирования о мутах в дискорд")]
                public LoggedFuncion LoggedMute = new LoggedFuncion();
                internal class LoggedFuncion
                {
                    [JsonProperty(LanguageEn ? "Support for logging the last N messages (Discord logging about mutes must be enabled)" : "Поддержка логирования последних N сообщений (Должно быть включено логирование в дискорд о мутах)")]
                    public Boolean UseHistoryMessage;
                    [JsonProperty(LanguageEn ? "How many latest player messages to send in logging" : "Сколько последних сообщений игрока отправлять в логировании")]
                    public Int32 CountHistoryMessage;
                }

                [JsonProperty(LanguageEn ? "Reasons to block chat" : "Причины для блокировки чата")]
                public List<Muted> MuteChatReasons = new List<Muted>();
                [JsonProperty(LanguageEn ? "Reasons to block your voice" : "Причины для блокировки голоса")]
                public List<Muted> MuteVoiceReasons = new List<Muted>();
                internal class Muted
                {
                    [JsonProperty(LanguageEn ? "Reason for blocking" : "Причина для блокировки")]
                    public String Reason;
                    [JsonProperty(LanguageEn ? "Block time (in seconds)" : "Время блокировки(в секундах)")]
                    public Int32 SecondMute;
                }
            }
            
                        [JsonProperty(LanguageEn ? "Configuring Message Processing" : "Настройка обработки сообщений")]
            public ControllerMessage ControllerMessages = new ControllerMessage();
            internal class ControllerMessage
            {
                [JsonProperty(LanguageEn ? "Basic settings for chat messages from the plugin" : "Основная настройка сообщений в чат от плагина")]
                public GeneralSettings GeneralSetting = new GeneralSettings();
                [JsonProperty(LanguageEn ? "Configuring functionality switching in chat" : "Настройка переключения функционала в чате")]
                public TurnedFuncional TurnedFunc = new TurnedFuncional();
                [JsonProperty(LanguageEn ? "Player message formatting settings" : "Настройка форматирования сообщений игроков")]
                public FormattingMessage Formatting = new FormattingMessage();
		   		 		  						  	   		  	  			  	  			  	 				  	  	
                internal class GeneralSettings
                {
                    [JsonProperty(LanguageEn ? "Customizing the chat alert format" : "Настройка формата оповещения в чате")]
                    public BroadcastSettings BroadcastFormat = new BroadcastSettings();
                    [JsonProperty(LanguageEn ? "Setting the mention format in the chat, via @" : "Настройка формата упоминания в чате, через @")]
                    public AlertSettings AlertFormat = new AlertSettings();
                    [JsonProperty(LanguageEn ? "Additional setting" : "Дополнительная настройка")]
                    public OtherSettings OtherSetting = new OtherSettings();
		   		 		  						  	   		  	  			  	  			  	 				  	  	
                    internal class BroadcastSettings
                    {
                        [JsonProperty(LanguageEn ? "The name of the notification in the chat" : "Наименование оповещения в чат")]
                        public String BroadcastTitle;
                        [JsonProperty(LanguageEn ? "Chat alert message color" : "Цвет сообщения оповещения в чат")]
                        public String BroadcastColor;
                        [JsonProperty(LanguageEn ? "Steam64ID for chat avatar" : "Steam64ID для аватарки в чате")]
                        public String Steam64IDAvatar;
                    }
                    internal class AlertSettings
                    {
                        [JsonProperty(LanguageEn ? "The color of the player mention message in the chat" : "Цвет сообщения упоминания игрока в чате")]
                        public String AlertPlayerColor;
                        [JsonProperty(LanguageEn ? "Sound when receiving and sending a mention via @" : "Звук при при получении и отправки упоминания через @")]
                        public String SoundAlertPlayer;
                    }
                    internal class OtherSettings
                    {
                        [JsonProperty(LanguageEn ? "Time after which the message will be deleted from the UI from the administrator" : "Время,через которое удалится сообщение с UI от администратора")]
                        public Int32 TimeDeleteAlertUI;

                        [JsonProperty(LanguageEn ? "The size of the message from the player in the chat" : "Размер сообщения от игрока в чате")]
                        public Int32 SizeMessage = 14;
                        [JsonProperty(LanguageEn ? "Player nickname size in chat" : "Размер ника игрока в чате")]
                        public Int32 SizeNick = 14;
                        [JsonProperty(LanguageEn ? "The size of the player's prefix in the chat (will be used if <size=N></size> is not set in the prefix itself)" : "Размер префикса игрока в чате (будет использовано, если в самом префиксе не установвлен <size=N></size>)")]
                        public Int32 SizePrefix = 14;
                    }
                }
                internal class TurnedFuncional
                {
                    [JsonProperty(LanguageEn ? "Configuring spam protection" : "Настройка защиты от спама")]
                    public AntiSpam AntiSpamSetting = new AntiSpam();
                    [JsonProperty(LanguageEn ? "Setting up a temporary chat block for newbies (who have just logged into the server)" : "Настройка временной блокировки чата новичкам (которые только зашли на сервер)")]
                    public AntiNoob AntiNoobSetting = new AntiNoob();
                    [JsonProperty(LanguageEn ? "Setting up private messages" : "Настройка личных сообщений")]
                    public PM PMSetting = new PM();

                    internal class AntiNoob
                    {
                        [JsonProperty(LanguageEn ? "Newbie protection in PM/R" : "Защита от новичка в PM/R")]
                        public Settings AntiNoobPM = new Settings();
                        [JsonProperty(LanguageEn ? "Newbie protection in global and team chat" : "Защита от новичка в глобальном и коммандном чате")]
                        public Settings AntiNoobChat = new Settings();
                        internal class Settings
                        {
                            [JsonProperty(LanguageEn ? "Enable protection?" : "Включить защиту?")]
                            public Boolean AntiNoobActivate = false;
                            [JsonProperty(LanguageEn ? "Newbie Chat Lock Time" : "Время блокировки чата для новичка")]
                            public Int32 TimeBlocked = 1200;
                        }
                    }
                    internal class AntiSpam
                    {
                        [JsonProperty(LanguageEn ? "Enable spam protection (Anti-spam)" : "Включить защиту от спама (Анти-спам)")]
                        public Boolean AntiSpamActivate;
                        [JsonProperty(LanguageEn ? "Time after which a player can send a message (AntiSpam)" : "Время через которое игрок может отправлять сообщение (АнтиСпам)")]
                        public Int32 FloodTime;
                        [JsonProperty(LanguageEn ? "Additional Anti-Spam settings" : "Дополнительная настройка Анти-Спама")]
                        public AntiSpamDuples AntiSpamDuplesSetting = new AntiSpamDuples();
                        internal class AntiSpamDuples
                        {
                            [JsonProperty(LanguageEn ? "Enable additional spam protection (Anti-duplicates, duplicate messages)" : "Включить дополнительную защиту от спама (Анти-дубликаты, повторяющие сообщения)")]
                            public Boolean AntiSpamDuplesActivate = true;
                            [JsonProperty(LanguageEn ? "How many duplicate messages does a player need to make to be confused by the system" : "Сколько дублирующих сообщений нужно сделать игроку чтобы его замутила система")]
                            public Int32 TryDuples = 3;
                            [JsonProperty(LanguageEn ? "Setting up automatic muting for duplicates" : "Настройка автоматического мута за дубликаты")]
                            public ControllerMute.Muted MuteSetting = new ControllerMute.Muted
                            {
                                Reason = LanguageEn ? "Blocking for duplicate messages (SPAM)" : "Блокировка за дублирующие сообщения (СПАМ)",
                                SecondMute = 300,
                            };
                        }
                    }
                    internal class PM
                    {
                        [JsonProperty(LanguageEn ? "Enable Private Messages" : "Включить личные сообщения")]
                        public Boolean PMActivate;
                        [JsonProperty(LanguageEn ? "Sound when receiving a private message" : "Звук при при получении личного сообщения")]
                        public String SoundPM;
                    }
                    [JsonProperty(LanguageEn ? "Enable PM ignore for players (/ignore nick or via interface)" : "Включить игнор ЛС игрокам(/ignore nick или через интерфейс)")]
                    public Boolean IgnoreUsePM;
                    [JsonProperty(LanguageEn ? "Hide the issue of items to the Admin from the chat" : "Скрыть из чата выдачу предметов Админу")]
                    public Boolean HideAdminGave;
                    [JsonProperty(LanguageEn ? "Move mute to team chat (In case of a mute, the player will not be able to write even to the team chat)" : "Переносить мут в командный чат(В случае мута, игрок не сможет писать даже в командный чат)")]
                    public Boolean MuteTeamChat;
                }
                internal class FormattingMessage
                {
                    [JsonProperty(LanguageEn ? "Enable message formatting [Will control caps, message format] (true - yes/false - no)" : "Включить форматирование сообщений [Будет контроллировать капс, формат сообщения] (true - да/false - нет)")]
                    public Boolean FormatMessage;
                    [JsonProperty(LanguageEn ? "Use a list of banned words (true - yes/false - no)" : "Использовать список запрещенных слов (true - да/false - нет)")]
                    public Boolean UseBadWords;
                    [JsonProperty(LanguageEn ? "The word that will replace the forbidden word" : "Слово которое будет заменять запрещенное слово")]
                    public String ReplaceBadWord;
                    [JsonProperty(LanguageEn ? "List of banned words" : "Список запрещенных слов")]
                    public List<String> BadWords = new List<String>();

                    [JsonProperty(LanguageEn ? "Nickname controller setup" : "Настройка контроллера ников")]
                    public NickController ControllerNickname = new NickController();
                    internal class NickController
                    {
                        [JsonProperty(LanguageEn ? "Enable player nickname formatting (message formatting must be enabled)" : "Включить форматирование ников игроков (должно быть включено форматирование сообщений)")]
                        public Boolean UseNickController = true;
                        [JsonProperty(LanguageEn ? "The word that will replace the forbidden word (You can leave it blank and it will just delete)" : "Слово которое будет заменять запрещенное слово (Вы можете оставить пустым и будет просто удалять)")]
                        public String ReplaceBadNick = "****";
                        [JsonProperty(LanguageEn ? "List of banned nicknames" : "Список запрещенных ников")]
                        public List<String> BadNicks = new List<String>();
                    }
                }
            }
		   		 		  						  	   		  	  			  	  			  	 				  	  	
            
            
            [JsonProperty(LanguageEn ? "Setting up chat alerts" : "Настройка оповещений в чате")]
            public ControllerAlert ControllerAlertSetting;

            internal class ControllerAlert
            {
                [JsonProperty(LanguageEn ? "Setting up chat alerts" : "Настройка оповещений в чате")]
                public Alert AlertSetting;
                [JsonProperty(LanguageEn ? "Setting notifications about the status of the player's session" : "Настройка оповещений о статусе сессии игрока")]
                public PlayerSession PlayerSessionSetting;
                [JsonProperty(LanguageEn ? "Configuring administrator session status alerts" : "Настройка оповещений о статусе сессии администратора")]
                public AdminSession AdminSessionSetting;
                [JsonProperty(LanguageEn ? "Setting up personal notifications to the player when connecting" : "Настройка персональных оповоещений игроку при коннекте")]
                public PersonalAlert PersonalAlertSetting;
                internal class Alert
                {
                    [JsonProperty(LanguageEn ? "Enable automatic messages in chat (true - yes/false - no)" : "Включить автоматические сообщения в чат (true - да/false - нет)")]
                    public Boolean AlertMessage;
                    [JsonProperty(LanguageEn ? "Type of automatic messages : true - sequential / false - random" : "Тип автоматических сообщений : true - поочередные/false - случайные")]
                    public Boolean AlertMessageType;

                    [JsonProperty(LanguageEn ? "List of automatic messages in chat" : "Список автоматических сообщений в чат")]
                    public List<String> MessageList;
                    [JsonProperty(LanguageEn ? "Interval for sending messages to chat (Broadcaster) (in seconds)" : "Интервал отправки сообщений в чат (Броадкастер) (в секундах)")]
                    public Int32 MessageListTimer;
                }
                internal class PlayerSession
                {
                    [JsonProperty(LanguageEn ? "When a player is notified about the entry / exit of the player, display his avatar opposite the nickname (true - yes / false - no)" : "При уведомлении о входе/выходе игрока отображать его аватар напротив ника (true - да/false - нет)")]
                    public Boolean ConnectedAvatarUse;

                    [JsonProperty(LanguageEn ? "Notify in chat when a player enters (true - yes/false - no)" : "Уведомлять в чате о входе игрока (true - да/false - нет)")]
                    public Boolean ConnectedAlert;
                    [JsonProperty(LanguageEn ? "Enable random notifications when a player from the list enters (true - yes / false - no)" : "Включить случайные уведомления о входе игрока из списка (true - да/false - нет)")]
                    public Boolean ConnectionAlertRandom;
                    [JsonProperty(LanguageEn ? "Show the country of the entered player (true - yes/false - no)" : "Отображать страну зашедшего игрока (true - да/false - нет")]
                    public Boolean ConnectedWorld;

                    [JsonProperty(LanguageEn ? "Notify when a player enters the chat (selected from the list) (true - yes/false - no)" : "Уведомлять о выходе игрока в чат(выбираются из списка) (true - да/false - нет)")]
                    public Boolean DisconnectedAlert;
                    [JsonProperty(LanguageEn ? "Enable random player exit notifications (true - yes/false - no)" : "Включить случайные уведомления о выходе игрока (true - да/false - нет)")]
                    public Boolean DisconnectedAlertRandom;
                    [JsonProperty(LanguageEn ? "Display reason for player exit (true - yes/false - no)" : "Отображать причину выхода игрока (true - да/false - нет)")]
                    public Boolean DisconnectedReason;

                    [JsonProperty(LanguageEn ? "Random player entry notifications({0} - player's nickname, {1} - country (if country display is enabled)" : "Случайные уведомления о входе игрока({0} - ник игрока, {1} - страна(если включено отображение страны)")]
                    public List<String> RandomConnectionAlert = new List<String>();
                    [JsonProperty(LanguageEn ? "Random notifications about the exit of the player ({0} - player's nickname, {1} - the reason for the exit (if the reason is enabled)" : "Случайные уведомления о выходе игрока({0} - ник игрока, {1} - причина выхода(если включена причина)")]
                    public List<String> RandomDisconnectedAlert = new List<String>();
                }
                internal class AdminSession
                {
                    [JsonProperty(LanguageEn ? "Notify admin on the server in the chat (true - yes/false - no)" : "Уведомлять о входе админа на сервер в чат (true - да/false - нет)")]
                    public Boolean ConnectedAlertAdmin;
                    [JsonProperty(LanguageEn ? "Notify about admin leaving the server in chat (true - yes/false - no)" : "Уведомлять о выходе админа на сервер в чат (true - да/false - нет)")]
                    public Boolean DisconnectedAlertAdmin;
                }
                internal class PersonalAlert
                {
                    [JsonProperty(LanguageEn ? "Enable random message to the player who has logged in (true - yes/false - no)" : "Включить случайное сообщение зашедшему игроку (true - да/false - нет)")]
                    public Boolean UseWelcomeMessage;
                    [JsonProperty(LanguageEn ? "List of messages to the player when entering" : "Список сообщений игроку при входе")]
                    public List<String> WelcomeMessage = new List<String>();
                }
            }

            
                        [JsonProperty(LanguageEn ? "Settings Rust+" : "Настройка Rust+")]
            public RustPlus RustPlusSettings;
            internal class RustPlus
            {
                [JsonProperty(LanguageEn ? "Use Rust+" : "Использовать Rust+")]
                public Boolean UseRustPlus;
                [JsonProperty(LanguageEn ? "Title for notification Rust+" : "Название для уведомления Rust+")]
                public String DisplayNameAlert;
            }
            
                        [JsonProperty(LanguageEn ? "Configuring support plugins" : "Настройка плагинов поддержки")]
            public ReferenceSettings ReferenceSetting = new ReferenceSettings();
            internal class ReferenceSettings
            {
                [JsonProperty(LanguageEn ? "Settings IQFakeActive" : "Настройка IQFakeActive")]
                public IQFakeActive IQFakeActiveSettings = new IQFakeActive();
                [JsonProperty(LanguageEn ? "Settings IQRankSystem" : "Настройка IQRankSystem")]
                public IQRankSystem IQRankSystems = new IQRankSystem();
                internal class IQRankSystem
                {
                    [JsonProperty(LanguageEn ? "Rank display format in chat ( {0} is the user's rank, do not delete this value)" : "Формат отображения ранга в чате ( {0} - это ранг юзера, не удаляйте это значение)")]
                    public String FormatRank = "[{0}]";
                    [JsonProperty(LanguageEn ? "Time display format with IQRank System in chat ( {0} is the user's time, do not delete this value)" : "Формат отображения времени с IQRankSystem в чате ( {0} - это время юзера, не удаляйте это значение)")]
                    public String FormatRankTime = "[{0}]";
                    [JsonProperty(LanguageEn ? "Use support IQRankSystem" : "Использовать поддержку рангов")]
                    public Boolean UseRankSystem;
                    [JsonProperty(LanguageEn ? "Show players their played time next to their rank" : "Отображать игрокам их отыгранное время рядом с рангом")]
                    public Boolean UseTimeStandart;
                }
                internal class IQFakeActive
                {
                    [JsonProperty(LanguageEn ? "Use support IQFakeActive" : "Использовать поддержку IQFakeActive")]
                    public Boolean UseIQFakeActive;
                }
            }
            
            
            [JsonProperty(LanguageEn ? "Setting up an answering machine" : "Настройка автоответчика")]
            public AnswerMessage AnswerMessages = new AnswerMessage();

            internal class AnswerMessage
            {
                [JsonProperty(LanguageEn ? "Enable auto-reply? (true - yes/false - no)" : "Включить автоответчик?(true - да/false - нет)")]
                public bool UseAnswer;
                [JsonProperty(LanguageEn ? "Customize Messages [Keyword] = Reply" : "Настройка сообщений [Ключевое слово] = Ответ")]
                public Dictionary<String, String> AnswerMessageList = new Dictionary<String, String>();
            }

            
                        [JsonProperty(LanguageEn ? "Additional setting" : "Дополнительная настройка")]
            public OtherSettings OtherSetting;

            internal class OtherSettings
            {
                [JsonProperty(LanguageEn ? "Setting up message logging" : "Настройка логирования сообщений")]
                public LoggedChat LogsChat = new LoggedChat();
                [JsonProperty(LanguageEn ? "Setting up logging of personal messages of players" : "Настройка логирования личных сообщений игроков")]
                public General LogsPMChat = new General();
                [JsonProperty(LanguageEn ? "Setting up chat/voice lock/unlock logging" : "Настройка логирования блокировок/разблокировок чата/голоса")]
                public General LogsMuted = new General();
                [JsonProperty(LanguageEn ? "Setting up logging of chat commands from players" : "Настройка логирования чат-команд от игроков")]
                public General LogsChatCommands = new General();
                internal class LoggedChat
                {
                    [JsonProperty(LanguageEn ? "Setting up general chat logging" : "Настройка логирования общего чата")]
                    public General GlobalChatSettings = new General();
                    [JsonProperty(LanguageEn ? "Setting up team chat logging" : "Настройка логирования тим чата")]
                    public General TeamChatSettings = new General();
                }
                internal class General
                {
                    [JsonProperty(LanguageEn ? "Enable logging (true - yes/false - no)" : "Включить логирование (true - да/false - нет)")]
                    public Boolean UseLogged = false;
                    [JsonProperty(LanguageEn ? "Webhooks channel for logging" : "Webhooks канала для логирования")]
                    public String Webhooks = "";
                }
            }
                    }

        
                public void BroadcastAuto()
        {
            Configuration.ControllerAlert.Alert Broadcast = config.ControllerAlertSetting.AlertSetting;

            if (Broadcast.AlertMessage)
            {
                Int32 IndexBroadkastNow = 0;
                String RandomMsg = String.Empty;

                timer.Every(Broadcast.MessageListTimer, () =>
                {
                    if (Broadcast.AlertMessageType)
                    {
                        foreach (BasePlayer p in BasePlayer.activePlayerList)
                        {
                            List<String> MessageList = GetMesagesList(p, Broadcast.MessageList.LanguageMessages);

                            if (IndexBroadkastNow >= MessageList.Count)
                                IndexBroadkastNow = 0;
                            RandomMsg = MessageList[IndexBroadkastNow];

                            ReplySystem(p, RandomMsg);
                        }

                        IndexBroadkastNow++;
                    }
                    else
                    {
                        foreach (BasePlayer p in BasePlayer.activePlayerList)
                            ReplySystem(p, GetMessages(p, Broadcast.MessageList.LanguageMessages));
                    }
                });

            }
        }
        internal class FlooderInfo
        {
            public Double Time;
            public String LastMessage;
            public Int32 TryFlood;
        }

        private String Format(Int32 units, String form1, String form2, String form3)
        {
            var tmp = units % 10;

            if (units >= 5 && units <= 20 || tmp >= 5 && tmp <= 9)
                return $"{units}{form1}";

            if (tmp >= 2 && tmp <= 4)
                return $"{units}{form2}";

            return $"{units}{form3}";
        }
        
        
        
        [ChatCommand("alert")]
        private void AlertChatCommand(BasePlayer Sender, String cmd, String[] args)
        {
            if (!permission.UserHasPermission(Sender.UserIDString, PermissionAlert)) return;
            Alert(Sender, args, false);
        }
        private void DrawUI_IQChat_Mute_And_Ignore_Player(BasePlayer player, SelectedAction Action, IEnumerable<BasePlayer> PlayerList, IEnumerable<FakePlayer> FakePlayerList = null)
        {
            User MyInfo = UserInformation[player.userID];
            if (MyInfo == null) return;
            Int32 X = 0, Y = 0;
            String ColorGreen = "0.5803922 1 0.5372549 1";
            String ColorRed = "0.8962264 0.2578764 0.3087685 1";
            String Color = String.Empty;

            if (IQFakeActive && FakePlayerList != null)
            {
                foreach (var playerInList in FakePlayerList)
                {
                    String Interface = InterfaceBuilder.GetInterface("UI_Chat_Mute_And_Ignore_Player");
                    if (Interface == null) return;

                    String DisplayName = playerInList.DisplayName;
                    if (GeneralInfo.RenameList.ContainsKey(playerInList.UserID))
                        if (!String.IsNullOrWhiteSpace(GeneralInfo.RenameList[playerInList.UserID].RenameNick))
                            DisplayName = GeneralInfo.RenameList[playerInList.UserID].RenameNick;

                    Interface = Interface.Replace("%OFFSET_MIN%", $"{-385.795 - (-281.17 * X)} {97.54 - (46.185 * Y)}");
                    Interface = Interface.Replace("%OFFSET_MAX%", $"{-186.345 - (-281.17 * X)} {132.03 - (46.185 * Y)}");
                    Interface = Interface.Replace("%DISPLAY_NAME%", $"{DisplayName}");
                    Interface = Interface.Replace("%COMMAND_ACTION%", $"newui.cmd action.mute.ignore ignore.and.mute.controller {Action} confirm.alert {playerInList.UserID}");

                    switch (Action)
                    {
                        case SelectedAction.Mute:
                            if (UserInformation.ContainsKey(playerInList.UserID) && UserInformation[playerInList.UserID] != null && (UserInformation[playerInList.UserID].MuteInfo.IsMute(MuteType.Chat) || UserInformation[playerInList.UserID].MuteInfo.IsMute(MuteType.Voice)))
                                Color = ColorRed;
                            else Color = ColorGreen;
                            break;
                        case SelectedAction.Ignore:
                            if (MyInfo.Settings.IsIgnored(playerInList.UserID))
                                Color = ColorRed;
                            else Color = ColorGreen;
                            break;
                        default:
                            break;
                    }

                    Interface = Interface.Replace("%COLOR%", Color);


                    X++;
                    if (X == 3)
                    {
                        X = 0;
                        Y++;
                    }

                    CuiHelper.AddUi(player, Interface);
                }
            }
            else
            {
                foreach (var playerInList in PlayerList)
                {
                    String Interface = InterfaceBuilder.GetInterface("UI_Chat_Mute_And_Ignore_Player");
                    if (Interface == null) return;
                    User Info = UserInformation[playerInList.userID];
                    if (Info == null) continue;

                    String DisplayName = playerInList.displayName;
                    if (GeneralInfo.RenameList.ContainsKey(playerInList.userID))
                        if (!String.IsNullOrWhiteSpace(GeneralInfo.RenameList[playerInList.userID].RenameNick))
                            DisplayName = GeneralInfo.RenameList[playerInList.userID].RenameNick;

                    Interface = Interface.Replace("%OFFSET_MIN%", $"{-385.795 - (-281.17 * X)} {97.54 - (46.185 * Y)}");
                    Interface = Interface.Replace("%OFFSET_MAX%", $"{-186.345 - (-281.17 * X)} {132.03 - (46.185 * Y)}");
                    Interface = Interface.Replace("%DISPLAY_NAME%", $"{DisplayName}");
                    Interface = Interface.Replace("%COMMAND_ACTION%", $"newui.cmd action.mute.ignore ignore.and.mute.controller {Action} confirm.alert {playerInList.userID}");

                    switch (Action)
                    {
                        case SelectedAction.Mute:
                            if (Info.MuteInfo.IsMute(MuteType.Chat) || Info.MuteInfo.IsMute(MuteType.Voice))
                                Color = ColorRed;
                            else Color = ColorGreen;
                            break;
                        case SelectedAction.Ignore:
                            if (MyInfo.Settings.IsIgnored(playerInList.userID))
                                Color = ColorRed;
                            else Color = ColorGreen;
                            break;
                        default:
                            break;
                    }

                    Interface = Interface.Replace("%COLOR%", Color);


                    X++;
                    if (X == 3)
                    {
                        X = 0;
                        Y++;
                    }

                    CuiHelper.AddUi(player, Interface);
                }
            }
        }

        public class Fields
        {
            public string name { get; set; }
            public string value { get; set; }
            public bool inline { get; set; }
            public Fields(string name, string value, bool inline)
            {
                this.name = name;
                this.value = value;
                this.inline = inline;
            }
        }
        private enum SelectedAction
        {
            Mute,
            Ignore
        }

        
                private void DrawUI_IQChat_DropList(BasePlayer player, String OffsetMin, String OffsetMax, String Title, TakeElementUser ElementType)
        {
            String Interface = InterfaceBuilder.GetInterface("UI_Chat_DropList");
            if (Interface == null) return;

            Interface = Interface.Replace("%TITLE%", Title);
            Interface = Interface.Replace("%OFFSET_MIN%", OffsetMin);
            Interface = Interface.Replace("%OFFSET_MAX%", OffsetMax);
            Interface = Interface.Replace("%BUTTON_DROP_LIST_CMD%", $"newui.cmd droplist.controller open {ElementType}");
		   		 		  						  	   		  	  			  	  			  	 				  	  	
            CuiHelper.AddUi(player, Interface);
        }

        private void RegisteredPermissions()
        {
            Configuration.ControllerParameters Controller = config.ControllerParameter;
            IEnumerable<Configuration.ControllerParameters.AdvancedFuncion> Parametres = Controller.Prefixes.Prefixes
                .Concat(Controller.NickColorList).Concat(Controller.MessageColorList);

            foreach (Configuration.ControllerParameters.AdvancedFuncion Permission in Parametres.Where(perm =>
                         !permission.PermissionExists(perm.Permissions, this)))
                permission.RegisterPermission(Permission.Permissions, this);

            if (!permission.PermissionExists(PermissionHideOnline, this))
                permission.RegisterPermission(PermissionHideOnline, this);
            if (!permission.PermissionExists(PermissionRename, this))
                permission.RegisterPermission(PermissionRename, this);
            if (!permission.PermissionExists(PermissionMute, this))
                permission.RegisterPermission(PermissionMute, this);
            if (!permission.PermissionExists(PermissionAlert, this))
                permission.RegisterPermission(PermissionAlert, this);
            if (!permission.PermissionExists(PermissionAntiSpam, this))
                permission.RegisterPermission(PermissionAntiSpam, this);
            if (!permission.PermissionExists(PermissionHideConnection, this))
                permission.RegisterPermission(PermissionHideConnection, this);
            if (!permission.PermissionExists(PermissionHideDisconnection, this))
                permission.RegisterPermission(PermissionHideDisconnection, this);
            if (!permission.PermissionExists(PermissionMutedAdmin, this))
                permission.RegisterPermission(PermissionMutedAdmin, this);

            PrintWarning("Permissions - completed");
        }

        private String GetLastMessage(BasePlayer player, Int32 Count)
        {
            String Messages = String.Empty;

            if (LastMessagesChat.ContainsKey(player))
            {
                foreach (String Message in LastMessagesChat[player].Skip(LastMessagesChat[player].Count - Count))
                    Messages += $"\n{Message}";
            }

            return Messages;
        }
        List<String> IQRankListKey(ulong userID) => (List<string>)(IQRankSystem?.Call("API_RANK_USER_KEYS", userID));
        void OnPlayerConnected(BasePlayer player)
        {
            UserConnecteionData(player);
            AlertController(player);
        }
        
        
        
                [ChatCommand("rename")]
        private void ChatCommandRename(BasePlayer Renamer, string command, string[] args)
        {
            if (!permission.UserHasPermission(Renamer.UserIDString, PermissionRename)) return;
            GeneralInformation General = GeneralInfo;
            if (General == null) return;

            if (Renamer == null)
            {
                ReplySystem(Renamer, LanguageEn ? "You can only use this command while on the server" : "Вы можете использовать эту команду только находясь на сервере");
                return;
            }
            if (args.Length == 0 || args == null)
            {
                ReplySystem(Renamer, lang.GetMessage("COMMAND_RENAME_NOTARG", this, Renamer.UserIDString));
                return;
            }

            String Name = args[0];
            UInt64 ID = Renamer.userID;
            if (args.Length == 2 && args[1] != null && !String.IsNullOrWhiteSpace(args[1]))
                if (!UInt64.TryParse(args[1], out ID))
                {
                    ReplySystem(Renamer, GetLang("COMMAND_RENAME_NOT_ID", Renamer.UserIDString));
                    return;
                }

            if (General.RenameList.ContainsKey(Renamer.userID))
            {
                General.RenameList[Renamer.userID].RenameNick = Name;
                General.RenameList[Renamer.userID].RenameID = ID;
            }
            else General.RenameList.Add(Renamer.userID, new GeneralInformation.RenameInfo { RenameNick = Name, RenameID = ID });

            ReplySystem(Renamer, GetLang("COMMAND_RENAME_SUCCES", Renamer.UserIDString, Name, ID));
            Renamer.displayName = Name;
        }

        public Dictionary<UInt64, FlooderInfo> Flooders = new Dictionary<UInt64, FlooderInfo>();
        [ConsoleCommand("saybro")]
        private void AlertOnlyPlayerConsoleCommand(ConsoleSystem.Arg args)
        {
            BasePlayer Sender = args.Player();
            if (Sender != null)
                if (!permission.UserHasPermission(Sender.UserIDString, PermissionAlert)) return;

            if (args.Args == null || args.Args.Length == 0)
            {
                if (Sender != null)
                    ReplySystem(Sender, LanguageEn ? "You didn't specify a player!" : "Вы не указали игрока!");
                else PrintWarning(LanguageEn ? "You didn't specify a player" : "Вы не указали игрока!");
                return;
            }
            BasePlayer Recipient = BasePlayer.Find(args.Args[0]);
            if (Recipient == null)
            {
                if (Sender != null)
                    ReplySystem(Sender, LanguageEn ? "The player is not on the server!" : "Игрока нет на сервере!");
                else PrintWarning(LanguageEn ? "The player is not on the server!" : "Игрока нет на сервере!");
                return;
            }
            Alert(Sender, Recipient, args.Args.Skip(1).ToArray());
        }
        private void OnUserConnected(IPlayer player) => ControlledBadNick(player);
        private const String PermissionMutedAdmin = "iqchat.adminmuted";

            }
}

