using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Oxide.Core.Plugins;
using Oxide.Core;
using Oxide.Game.Rust.Cui;
using ProtoBuf;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("SystemRPG", "Mareloy", "1.0.1")]

    public class SystemRPG : RustPlugin
    {
		private static ConfigData configData;
        private class ConfigData
        {
			[JsonProperty("ОСНОВНЫЕ НАСТРОЙКИ")]
            public BasicSetting OptionsBasic = new BasicSetting();
			[JsonProperty("НАСТРОЙКИ ПОЛУЧЕНИЯ ОПЫТА")]
            public ActionPoints ActionSettings = new ActionPoints();
			[JsonProperty("НАСТРОЙКИ ОПЫТА И ОПИСАНИЕ")]
            public Dictionary<string, Skill> SkillList = new Dictionary<string, Skill>();

            internal class BasicSetting 
            {
				[JsonProperty("Текст в шапке")]
                public string Descript = "Название сервера";
				[JsonProperty("Стартовое количество очков у игрока")]
                public int BasePoints = 0;
				[JsonProperty("Количество выдаваемого опыта каждую минуту")]
                public float TimePoints = 0.0f;
                [JsonProperty("Ссылка на картинку слева")]
                public string UrlImgLeft = "https://i.imgur.com/uMJxkcx.png";
                [JsonProperty("Ссылка на картинку с права")]
                public string UrlImgRight = "https://i.imgur.com/KSnYPLm.png";
			}

			internal class ActionPoints
            {
                [JsonProperty("Количество выдаваемого опыта за добычу ресурсов отбойным молотком")]
                public float GatherHitChainsaw = 0.00001f;
                [JsonProperty("Количество выдаваемого опыта за добычу ресурсов бензопилой")]
                public float GatherHitJackhammer = 0.00001f;
                [JsonProperty("Количество выдаваемого опыта за попадание по вертолёту")]
                public float HeliDamage = 0.0005f;
                [JsonProperty("Количество выдаваемого опыта за улучшение построек")]
                public float Upgrade = 0.005f;
                [JsonProperty("Количество выдаваемого опыта за попадание по танку")]
                public float TankDamage = 0.0005f;
                [JsonProperty("Количество выдаваемого опыта за убийство животных")]
                public float AnimalKill = 0.03f;
                [JsonProperty("Количество выдаваемого опыта за поднятие предмета")]
                public float PickUp = 0.001f;
                [JsonProperty("Количество выдаваемого опыта за добычу ресурсов")]
                public float GatherHit = 0.0002f;
                [JsonProperty("Количество выдаваемого опыта за убийство игрока")]
                public float PlayerKill = 0.02f;
				[JsonProperty("Количество выдаваемого опыта за разбитие бочек")]
				public float BarrelKill = 0.0002f;
                [JsonProperty("Количество выдаваемого опыта за убийство NPC")]
                public float NPCKill = 0.02f;

				// [JsonProperty("Количество выдаваемого стартового опыта за улучшение")]
                // public float BaseLearn = 0.002f;
                // [JsonProperty("Множитель за уровень чертежа")]
                // public float LearnIncrease = 0.0002f;
            }

            internal class Skill
            {
                [JsonProperty("Отображаемое название опыта")]
                public string DisplayName;
                [JsonProperty("Описание опыта")]
                public string Description;
                [JsonProperty("Поместить данный опыт в верху")]
                public bool IsSkill;
                [JsonProperty("Ссылка на картинку опыта")]
                public string SkillURL;
                [JsonProperty("Рейт опыта при повышении уровня |Кол-во уровней|")]
                public List<float> Increase = new List<float>();
            }

            public float GetIncreaseOf(string name, int level)
            {
                if (SkillList.ContainsKey(name))
                {
                    if (SkillList[name].Increase.Count >= level)
						return SkillList[name].Increase.ElementAt(level);
                }
                Interface.Oxide.LogWarning($"Try to get {level}LVL of {name} that is not exists!");
                return 0f;
            }

            public ConfigData()
            {
                ActionSettings = new ActionPoints();
				OptionsBasic = new BasicSetting();

				SkillList = new Dictionary<string, Skill>
                {
                    ["ШАХТЁР"] = new Skill
                    {
                        DisplayName = "ШАХТЁР",
                        Description = "С каждым уровнем увеличивается количество добываемых ресурсов киркой, чем выше уровень, \nтем больше вы будете добывать ресурсов.",
                        SkillURL = "https://imgur.com/ixHJgXc.png",
                        IsSkill = true,
                        Increase = new List<float>
                        {
                            1.0f, 1.05f, 1.1f, 1.15f, 1.2f, 1.25f, 1.3f, 1.35f, 1.4f, 1.45f, 1.5f, 1.55f, 1.6f, 1.65f, 1.7f,
							1.75f, 1.8f, 1.85f, 1.9f, 1.95f, 2.0f, 2.05f, 2.1f, 2.15f, 2.2f, 2.25f, 2.3f, 2.35f, 2.4f, 2.45f,
							2.5f, 2.55f, 2.6f, 2.65f, 2.7f, 2.75f, 2.8f, 2.85f, 2.9f, 2.95f, 3.0f, 3.05f, 3.1f, 3.15f, 3.2f,
							3.25f, 3.3f, 3.35f, 3.4f, 3.45f, 3.5f, 3.55f, 3.6f, 3.65f, 3.7f, 3.75f, 3.8f, 3.85f, 3.9f, 3.95f,
							4.0f, 4.05f, 4.1f, 4.15f, 4.2f, 4.25f, 4.3f, 4.35f, 4.4f, 4.45f, 4.5f, 4.55f, 4.6f, 4.65f, 4.7f,
                            4.75f, 4.8f, 4.85f, 4.9f, 4.95f, 5.0f
                        }
                    },
                    ["ДРОВОСЕК"] = new Skill
                    {
                        DisplayName = "ДРОВОСЕК",
                        Description = "Каждый уровень увеличивает количество добываемого дерева, \nчем выше ваш уровень, тем больше вы будете добывать дерева.",
                        SkillURL = "https://imgur.com/O5HCgDq.png",
                        IsSkill = true,
                        Increase = new List<float>
                        {
                            1.0f, 1.05f, 1.1f, 1.15f, 1.2f, 1.25f, 1.3f, 1.35f, 1.4f, 1.45f, 1.5f, 1.55f, 1.6f, 1.65f, 1.7f,
							1.75f, 1.8f, 1.85f, 1.9f, 1.95f, 2.0f, 2.05f, 2.1f, 2.15f, 2.2f, 2.25f, 2.3f, 2.35f, 2.4f, 2.45f,
							2.5f, 2.55f, 2.6f, 2.65f, 2.7f, 2.75f, 2.8f, 2.85f, 2.9f, 2.95f, 3.0f, 3.05f, 3.1f, 3.15f, 3.2f,
							3.25f, 3.3f, 3.35f, 3.4f, 3.45f, 3.5f, 3.55f, 3.6f, 3.65f, 3.7f, 3.75f, 3.8f, 3.85f, 3.9f, 3.95f,
							4.0f, 4.05f, 4.1f, 4.15f, 4.2f, 4.25f, 4.3f, 4.35f, 4.4f, 4.45f, 4.5f, 4.55f, 4.6f, 4.65f, 4.7f,
                            4.75f, 4.8f, 4.85f, 4.9f, 4.95f, 5.0f
                        }
                    },
                    ["ПЕЧНИК"] = new Skill
                    {
                        DisplayName = "ПЕЧНИК",
                        Description = "Скаждым уровнем увеличивается скорость плавки в печах, \nчем выше ваш уровень, тем быстрее работает печь.",
                        SkillURL = "https://imgur.com/UEYt0uM.png",
                        IsSkill = true,
                        Increase = new List<float>
                        {
                            1.0f, 1.05f, 1.1f, 1.15f, 1.2f, 1.25f, 1.3f, 1.35f, 1.4f, 1.45f, 1.5f, 1.55f, 1.6f, 1.65f, 1.7f,
							1.75f, 1.8f, 1.85f, 1.9f, 1.95f, 2.0f, 2.05f, 2.1f, 2.15f, 2.2f, 2.25f, 2.3f, 2.35f, 2.4f, 2.45f,
							2.5f, 2.55f, 2.6f, 2.65f, 2.7f, 2.75f, 2.8f, 2.85f, 2.9f, 2.95f, 3.0f, 3.05f, 3.1f, 3.15f, 3.2f,
							3.25f, 3.3f, 3.35f, 3.4f, 3.45f, 3.5f, 3.55f, 3.6f, 3.65f, 3.7f, 3.75f, 3.8f, 3.85f, 3.9f, 3.95f,
							4.0f, 4.05f, 4.1f, 4.15f, 4.2f, 4.25f, 4.3f, 4.35f, 4.4f, 4.45f, 4.5f, 4.55f, 4.6f, 4.65f, 4.7f,
                            4.75f, 4.8f, 4.85f, 4.9f, 4.95f, 5.0f
                        }
                    },
                    ["ОХОТНИК"] = new Skill
                    {
                        DisplayName = "ОХОТНИК",
                        Description = "Прокачка этого навыка позволит вам получать больше ресурсов, \nпри разделке животных.",
                        SkillURL = "https://imgur.com/BLQZBem.png",
                        IsSkill = true,
                        Increase = new List<float>
                        {
                            1.0f, 1.05f, 1.1f, 1.15f, 1.2f, 1.25f, 1.3f, 1.35f, 1.4f, 1.45f, 1.5f, 1.55f, 1.6f, 1.65f, 1.7f,
							1.75f, 1.8f, 1.85f, 1.9f, 1.95f, 2.0f, 2.05f, 2.1f, 2.15f, 2.2f, 2.25f, 2.3f, 2.35f, 2.4f, 2.45f,
							2.5f, 2.55f, 2.6f, 2.65f, 2.7f, 2.75f, 2.8f, 2.85f, 2.9f, 2.95f, 3.0f, 3.05f, 3.1f, 3.15f, 3.2f,
							3.25f, 3.3f, 3.35f, 3.4f, 3.45f, 3.5f, 3.55f, 3.6f, 3.65f, 3.7f, 3.75f, 3.8f, 3.85f, 3.9f, 3.95f,
							4.0f, 4.05f, 4.1f, 4.15f, 4.2f, 4.25f, 4.3f, 4.35f, 4.4f, 4.45f, 4.5f, 4.55f, 4.6f, 4.65f, 4.7f,
                            4.75f, 4.8f, 4.85f, 4.9f, 4.95f, 5.0f
                        }
                    },
                    ["ФЕРМЕР"] = new Skill
                    {
                        DisplayName = "ФЕРМЕР",
                        Description = "С каждым следующем уровнем вы будете получать больше ресурсов \nпри подборе их с земли.",
                        SkillURL = "https://imgur.com/Z81unSg.png",
                        IsSkill = true,
                        Increase = new List<float>
                        {
                            1.0f, 1.05f, 1.1f, 1.15f, 1.2f, 1.25f, 1.3f, 1.35f, 1.4f, 1.45f, 1.5f, 1.55f, 1.6f, 1.65f, 1.7f,
							1.75f, 1.8f, 1.85f, 1.9f, 1.95f, 2.0f, 2.05f, 2.1f, 2.15f, 2.2f, 2.25f, 2.3f, 2.35f, 2.4f, 2.45f,
							2.5f, 2.55f, 2.6f, 2.65f, 2.7f, 2.75f, 2.8f, 2.85f, 2.9f, 2.95f, 3.0f, 3.05f, 3.1f, 3.15f, 3.2f,
							3.25f, 3.3f, 3.35f, 3.4f, 3.45f, 3.5f, 3.55f, 3.6f, 3.65f, 3.7f, 3.75f, 3.8f, 3.85f, 3.9f, 3.95f,
							4.0f, 4.05f, 4.1f, 4.15f, 4.2f, 4.25f, 4.3f, 4.35f, 4.4f, 4.45f, 4.5f, 4.55f, 4.6f, 4.65f, 4.7f,
                            4.75f, 4.8f, 4.85f, 4.9f, 4.95f, 5.0f
                        }
                    },
                    ["СТРЕЛОК"] = new Skill
                    {
                        DisplayName = "СТРЕЛОК",
                        Description = "Каждый новый уровень вам даст способность наносить \nбольше урона игрокам при перестрелке.",
                        SkillURL = "https://imgur.com/mnHB2JQ.png",
                        IsSkill = false,
                        Increase = new List<float>
                        {
                            1.0f, 1.03f, 1.06f, 1.09f, 1.12f, 1.15f,
							1.18f, 1.21f, 1.24f, 1.27f, 1.3f, 1.33f,
							1.36f, 1.39f, 1.42f, 1.45f, 1.48f, 1.51f,
							1.54f, 1.57f, 1.6f
                        }
                    },
                    ["ЛОВКОСТЬ"] = new Skill
                    {
                        DisplayName = "ЛОВКОСТЬ",
                        Description = "Каждый уровень дает больше шанса уклониться от получения урона \nЧем выше ваш уровень, тем больше шанс уклона от пуль.",
                        SkillURL = "https://imgur.com/fJFd5ds.png",
                        IsSkill = false,
                        Increase = new List<float>
                        {
                            0.0f, 0.01f, 0.02f, 0.03f, 0.04f, 0.05f,
							0.06f, 0.07f, 0.08f, 0.09f, 0.1f,  0.11f,
							0.12f,  0.13f,  0.14f,  0.15f,  0.16f,
							0.17f,  0.18f,  0.19f,  0.25f
                        }
                    },
                    ["СИЛА"] = new Skill
                    {
                        DisplayName = "СИЛА",
                        Description = "С каждым новым уровнем вы будете получать меньше \nурона который вам наносят игроки и НПС.",
                        SkillURL = "https://imgur.com/q3rrvuK.png",
                        IsSkill = false,
                        Increase = new List<float>
                        {
                            0.0f, 0.01f, 0.02f, 0.03f, 0.04f, 0.05f, 0.06f, 0.07f, 0.08f, 0.09f, 0.1f,
                            0.11f, 0.12f, 0.13f, 0.14f, 0.15f, 0.16f, 0.17f, 0.18f, 0.19f, 0.25f
                        }
                    },
                    //["ИНТЕЛЛЕКТ"] = new Skill
                    //{
                    //    DisplayName = "ИНТЕЛЛЕКТ",
                    //    Description = "Улучшая эту способность вы получаете возможность \nбыстрее крафтить предметы. \nБесполезный навык на нашем сервере.",
                    //    SkillURL = "https://imgur.com/l75dJCH.png",
                    //    IsSkill = false,
                    //    Increase = new List<float>
                    //    {
                    //        0.0f, 0.03f, 0.06f, 0.09f, 0.12f, 0.15f, 0.18f, 0.21f, 0.24f, 0.27f, 0.3f,
                    //        0.33f, 0.36f, 0.39f, 0.42f, 0.45f, 0.48f, 0.51f, 0.54f, 0.57f, 0.6f
                    //    }
                    //},
					["МЕТАБОЛИЗМ"] = new Skill
                    {
                        DisplayName = "МЕТАБОЛИЗМ",
                        Description = "Улучшая эту способность вы получаете возможность \nбыстрее крафтить предметы. \nБесполезный навык на нашем сервере.",
                        SkillURL = "https://imgur.com/l75dJCH.png",
                        IsSkill = false,
                        Increase = new List<float>
                        {
                            0.0f, 0.03f, 0.06f, 0.09f, 0.12f, 0.15f, 0.18f, 0.21f, 0.24f, 0.27f, 0.3f,
                            0.33f, 0.36f, 0.39f, 0.42f, 0.45f, 0.48f, 0.51f, 0.54f, 0.57f, 0.6f
                        }
                    },
                    ["ВАМПИРИЗМ"] = new Skill
                    {
                        DisplayName = "ВАМПИРИЗМ",
                        Description = "Каждый новый уровень вам даст способность регенирировать \nсвое здоровье при нанесении урона игрокам.",
                        SkillURL = "https://imgur.com/myVICVD.png",
                        IsSkill = false,
                        Increase = new List<float>
                        {
                            0.0f, 1.0f, 1.5f, 2.0f, 2.5f, 3.0f, 3.5f, 4.0f, 4.5f, 5.0f, 5.5f,
							6.0f, 6.5f, 7.0f, 7.5f, 8.0f, 8.5f, 9.0f, 9.5f, 10.0f
                        }
                    },
                };
            }
        }

     #region Variables

        private static Dictionary<ulong, PlayerInfo> PlayerInfos = new Dictionary<ulong, PlayerInfo>();

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                configData = Config.ReadObject<ConfigData>();
                if (configData?.SkillList == null) LoadDefaultConfig();
            }
            catch
            {
                PrintWarning($"Ошибка чтения конфигурации 'oxide/configData/{Name}', создаём новую конфигурацию!!");
                LoadDefaultConfig();
            }

            NextTick(SaveConfig);
        }
        protected override void LoadDefaultConfig() => configData = new ConfigData();
        protected override void SaveConfig() => Config.WriteObject(configData);

     #endregion
     #region Classes

        private class PlayerInfo
        {
            internal class SkillInfo
            {
                public Dictionary<string, int> Skills = new Dictionary<string, int>();
                public float GetCurrentIncrease(string name) => configData.GetIncreaseOf(name, Skills[name]);
            }

            public float CurrentPoints;
            public SkillInfo SkillsInfo = new SkillInfo();

            public void AddPoints(BasePlayer player, float increase)
            {
                CurrentPoints += increase;
                if (player != null)
                {
                    UI_DrawCurrentInfo(player, Math.Floor(CurrentPoints) > Math.Floor(CurrentPoints - increase));
                }
            }

            public bool AddLevel(BasePlayer player, string name)
            {
                if (CurrentPoints < 1)
                {
                    UI_DrawResearch(player, name + "ERROR");
                    return false;
                }

                if (SkillsInfo.Skills[name] + 1 >= configData.SkillList[name].Increase.Count)
                {
                    UI_DrawResearch(player, name + "UPER");
                    return false;
                }

                SkillsInfo.Skills[name]++;
                UI_DrawResearch(player, name);
                CurrentPoints--;
                return true;
            }
        }

     #endregion
     #region Initialization

	    static SystemRPG ins;
        [PluginReference] Plugin XMenu;
        Timer TimerInitialize;
        private void OnServerInitialized()
        {
            ins = this;
            if (Interface.Oxide.DataFileSystem.ExistsDatafile("PluginDataBase/SystemRPG/SystemRPG_Player"))
				PlayerInfos = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, PlayerInfo>>("PluginDataBase/SystemRPG/SystemRPG_Player");

            TimerInitialize = timer.Every(5f, () =>
            {
                if (XMenu.IsLoaded)
                {
                    XMenu.Call("API_RegisterMenu", this.Name, "rpg", "assets/icons/study.png", "RenderRpg", null);

                    cmd.AddChatCommand("rpg", this, (p, cmd, args) => rust.RunClientCommand(p, "custommenu true rpg"));
                    TimerInitialize.Destroy();
                }
            });

            InitFileManager();
			ServerMgr.Instance.StartCoroutine(LoadImages());

            foreach (var check in BasePlayer.activePlayerList)
                OnPlayerConnected(check);

            if (configData.OptionsBasic.TimePoints > 0)
            {
                timer.Every(60, () =>
                {
                    foreach (var basePlayer in BasePlayer.activePlayerList.Where(p => !p.IsReceivingSnapshot))
                    {
                        PlayerInfos[basePlayer.userID].AddPoints(basePlayer, configData.OptionsBasic.TimePoints);
                    }
                });
            }

            foreach (var imagesKey in configData.SkillList)
            {
                if (!string.IsNullOrEmpty(imagesKey.Value.SkillURL))
                    ServerMgr.Instance.StartCoroutine(m_FileManager.LoadFile(imagesKey.Key, imagesKey.Value.SkillURL));
            }
        }

        private void SaveData() => Interface.Oxide.DataFileSystem.WriteObject("PluginDataBase/SystemRPG/SystemRPG_Player", PlayerInfos);
        void OnServerSave() => SaveData();

        private void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, "GUI_SystemRPG_Lvl");
                CuiHelper.DestroyUi(player, "GUI_SystemRPG_Menu");
                CuiHelper.DestroyUi(player, "GUI_SystemRPG_Lvl" + ".CE");
                CuiHelper.DestroyUi(player, "GUI_SystemRPG_Lvl" + ".CL");
            }
            SaveData();
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            if (player.IsReceivingSnapshot)
            {
                timer.In(1f, () => OnPlayerConnected(player));
                return;
            }

            if (!PlayerInfos.ContainsKey(player.userID))
            {
                PlayerInfos.Add(player.userID, new PlayerInfo { CurrentPoints = configData.OptionsBasic.BasePoints });
                PlayerInfos[player.userID].SkillsInfo.Skills = configData.SkillList.ToDictionary(p => p.Key, p => 0); // 1
            }

            CuiHelper.DestroyUi(player, LayerInfo);
            CuiElementContainer container = new CuiElementContainer();

			container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-235 16", OffsetMax = "-210 98" },
                Button = { Color = HexToRustFormat("#8E8E8E40"), Command = "chat.say /rpg" },
                Text = { Text = "" }
            }, "Hud", LayerInfo);

			timer.Once(5f, () =>
			{
			    CuiHelper.AddUi(player, container);
			    UI_DrawCurrentInfo(player);
			});
        }

     #endregion
     #region Hooks

        // private void OnItemCraftFinished(ItemCraftTask task, Item item)
        // {
        //     if (task.owner != null)
        //     {
        //         BasePlayer player = task.owner;
        //         if (PlayerInfos.ContainsKey(player.userID))
        //         {
        //             PlayerInfos[player.userID].AddPoints(player, configData.ActionSettings.BaseLearn * configData.ActionSettings.LearnIncrease * task.blueprint.workbenchLevelRequired);
        //         }
        //     }
        //     return;
        // }

        //object OnItemCraft(ItemCraftTask task, BasePlayer crafter)
        //{
        //    if (PlayerInfos.ContainsKey(crafter.userID))
        //    {
        //        var curvalue = PlayerInfos[crafter.userID].SkillsInfo.GetCurrentIncrease("ИНТЕЛЛЕКТ");
        //        var craftingTime = task.blueprint.time;
        //        var amountToReduce = task.blueprint.time * curvalue;
        //        craftingTime -= amountToReduce;
        //
        //        if (craftingTime < 0)
        //            craftingTime = 0;
        //
        //        if (!task.blueprint.name.Contains("(Clone)"))
        //            task.blueprint = UnityEngine.Object.Instantiate(task.blueprint);
        //
        //        task.blueprint.time = craftingTime;
        //        return null;
        //    }
        //    return null;
        //}

        object OnOvenToggle(BaseOven oven, BasePlayer player)
        {
            if (oven.HasFlag(BaseEntity.Flags.On)) return null;

            if (oven.ShortPrefabName.Contains("furnace"))
            {
                double rate = PlayerInfos[player.userID].SkillsInfo.GetCurrentIncrease("ПЕЧНИК");
                StartCooking(oven, oven.GetComponent<BaseEntity>(), rate);
                return false;
            }
            return null;
        }

        void StartCooking(BaseOven oven, BaseEntity entity, double ovenMultiplier)
        {
            if (FindBurnable(oven) == null)
                return;
            oven.inventory.temperature = 1000f;
            oven.UpdateAttachmentTemperature();
            InvokeHandler.CancelInvoke(entity.GetComponent<MonoBehaviour>(), new Action(oven.Cook));
            InvokeHandler.InvokeRepeating(entity.GetComponent<MonoBehaviour>(), new Action(oven.Cook), (float)(1f / ovenMultiplier), (float)(1f / ovenMultiplier));
            entity.SetFlag(BaseEntity.Flags.On, true, false);
        }

        Item FindBurnable(BaseOven oven)
        {
            if (oven.inventory == null)
                return null;
            foreach (Item current in oven.inventory.itemList)
            {
                ItemModBurnable component = current.info.GetComponent<ItemModBurnable>();
                if (component && (oven.fuelType == null || current.info == oven.fuelType))
                    return current;
            }
            return null;
        }

        List<LootContainer> handledContainers = new List<LootContainer>();
        private void CanAffordUpgrade(BasePlayer player, BuildingBlock block, BuildingGrade.Enum grade)
        {
            if (PlayerInfos.ContainsKey(player.userID))
            {
                PlayerInfos[player.userID].AddPoints(player, configData.ActionSettings.Upgrade);
            }
        }

        private object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (entity is BasePlayer && entity.GetComponent<NPCPlayer>() == null && !entity.IsNpc)
            {
                BasePlayer player = entity as BasePlayer;
                var attacker = info.InitiatorPlayer;
                if (UnityEngine.Random.Range(0f, 1f) < PlayerInfos[player.userID].SkillsInfo.GetCurrentIncrease("ЛОВКОСТЬ"))
                {
                    info.damageTypes.ScaleAll(0);
                    return false;
                }
                if (PlayerInfos.ContainsKey(player.userID))
                {
                    float decrease = PlayerInfos[player.userID].SkillsInfo.GetCurrentIncrease("СИЛА");
                    info.damageTypes.ScaleAll(1 - decrease);
                }
            }
            if (info.InitiatorPlayer != null && PlayerInfos.ContainsKey(info.InitiatorPlayer.userID))
            {
                if (entity is LootContainer || entity is BuildingBlock) return null;
                float increase = PlayerInfos[info.InitiatorPlayer.userID].SkillsInfo.GetCurrentIncrease("ВАМПИРИЗМ");
                NextTick(() =>
                {
                    var damage = System.Convert.ToInt32(Math.Round(info.damageTypes.Total(), 0, MidpointRounding.AwayFromZero));
                    var heal = damage * (increase/100);
                    info.InitiatorPlayer.health += heal;
                });

            }
            if (info != null && info.Initiator is BasePlayer && !info.Initiator.IsNpc && info.Initiator.GetComponent<NPCPlayer>() == null)
            {
                if (PlayerInfos.ContainsKey(info.Initiator.GetComponent<BasePlayer>().userID))
                {
					if (entity as BaseCorpse != null)
                        return null;
                    BasePlayer player = info.Initiator.GetComponent<BasePlayer>();
                    float increase = PlayerInfos[player.userID].SkillsInfo.GetCurrentIncrease("СТРЕЛОК");
                    info.damageTypes.ScaleAll(increase);

                    if (entity is BaseHelicopter)
                    {
                        PlayerInfos[player.userID].AddPoints(player, configData.ActionSettings.HeliDamage);
                    }
                    if (entity is BradleyAPC)
                    {
                        PlayerInfos[player.userID].AddPoints(player, configData.ActionSettings.TankDamage);
                    }
                }
            }
            return null;
        }

		private void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            if (entity != null && entity is BasePlayer)
            {
                BasePlayer player = entity as BasePlayer;
                if (PlayerInfos.ContainsKey(player.userID))
                {
					float increase = 1f;

					var activeitem = player.GetActiveItem();
				    if (activeitem != null && activeitem.info.shortname.Contains("jackhammer"))
					{
						PlayerInfos[player.userID].AddPoints(player, configData.ActionSettings.GatherHitJackhammer);
						if (dispenser.gatherType == ResourceDispenser.GatherType.Tree)
                        {
                            increase = PlayerInfos[player.userID].SkillsInfo.GetCurrentIncrease("ДРОВОСЕК");
                        }
                        else if (dispenser.gatherType == ResourceDispenser.GatherType.Ore)
                        {
                            increase = PlayerInfos[player.userID].SkillsInfo.GetCurrentIncrease("ШАХТЁР");
                        }
                        item.amount = (int)Math.Floor(item.amount * increase);
					}
					if (activeitem != null && activeitem.info.shortname.Contains("chainsaw"))
					{
						PlayerInfos[player.userID].AddPoints(player, configData.ActionSettings.GatherHitChainsaw);
						if (dispenser.gatherType == ResourceDispenser.GatherType.Tree)
                        {
                            increase = PlayerInfos[player.userID].SkillsInfo.GetCurrentIncrease("ДРОВОСЕК");
                        }
                        else if (dispenser.gatherType == ResourceDispenser.GatherType.Ore)
                        {
                            increase = PlayerInfos[player.userID].SkillsInfo.GetCurrentIncrease("ШАХТЁР");
                        }
                        item.amount = (int)Math.Floor(item.amount * increase);
					}

					PlayerInfos[player.userID].AddPoints(player, configData.ActionSettings.GatherHit);
                    if (dispenser.gatherType == ResourceDispenser.GatherType.Tree)
                    {
                        increase = PlayerInfos[player.userID].SkillsInfo.GetCurrentIncrease("ДРОВОСЕК");
                    }
                    else if (dispenser.gatherType == ResourceDispenser.GatherType.Ore)
                    {
                        increase = PlayerInfos[player.userID].SkillsInfo.GetCurrentIncrease("ШАХТЁР");
                    }
                    else if (dispenser.gatherType == ResourceDispenser.GatherType.Flesh)
                    {
                        increase = PlayerInfos[player.userID].SkillsInfo.GetCurrentIncrease("ОХОТНИК");
                    }
                    item.amount = (int)Math.Floor(item.amount * increase);
                }
            }
            return;
        }

        void OnDispenserBonus(ResourceDispenser dispenser, BasePlayer player, Item item)
        {
            if (PlayerInfos.ContainsKey(player.userID))
            {
                PlayerInfos[player.userID].AddPoints(player, configData.ActionSettings.GatherHit);

                float increase = 1f;
                if (dispenser.gatherType == ResourceDispenser.GatherType.Tree)
                {
                    increase = PlayerInfos[player.userID].SkillsInfo.GetCurrentIncrease("ДРОВОСЕК");
                }
                else if (dispenser.gatherType == ResourceDispenser.GatherType.Ore)
                {
                    increase = PlayerInfos[player.userID].SkillsInfo.GetCurrentIncrease("ШАХТЁР");
                }
                else if (dispenser.gatherType == ResourceDispenser.GatherType.Flesh)
                {
                    increase = PlayerInfos[player.userID].SkillsInfo.GetCurrentIncrease("ОХОТНИК");
                }

                item.amount = (int)Math.Floor(item.amount * increase);
            }
            return;
        }

        private void OnCollectiblePickup(Item item, BasePlayer player)
        {
            PlayerInfos[player.userID].AddPoints(player, configData.ActionSettings.PickUp);
            float increase = PlayerInfos[player.userID].SkillsInfo.GetCurrentIncrease("ФЕРМЕР");
            item.amount = (int)Math.Floor(item.amount * increase);
            return;
        }

        void OnGrowableGather(GrowableEntity plant, Item item, BasePlayer player)
        {
            PlayerInfos[player.userID].AddPoints(player, configData.ActionSettings.PickUp);
            float increase = PlayerInfos[player.userID].SkillsInfo.GetCurrentIncrease("ФЕРМЕР");
            item.amount = (int)Math.Floor(item.amount * increase);
            return;
        } 

        void OnQuarryGather(MiningQuarry quarry, Item item)
        {
            if (quarry.OwnerID == 0) return;
            int newAmount = (int)(item.amount * PlayerInfos[quarry.OwnerID].SkillsInfo.GetCurrentIncrease("ШАХТЁР"));
            item.amount = newAmount > 1 ? newAmount : 1;
        }

        private void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (info != null && info.Initiator is BasePlayer && !info.Initiator.IsNpc && info.Initiator.GetComponent<BasePlayer>().IsConnected)
            {
                BasePlayer initiator = info.Initiator as BasePlayer;
				if(info.InitiatorPlayer != null && info.InitiatorPlayer == entity)
                    return;
                if (entity is BasePlayer && !entity.IsNpc)
                    PlayerInfos[initiator.userID].AddPoints(initiator, configData.ActionSettings.PlayerKill);
                if (entity.GetComponent<BaseAnimalNPC>() != null)
                    PlayerInfos[initiator.userID].AddPoints(initiator, configData.ActionSettings.AnimalKill);
                if (entity.GetComponent<BaseNPC>() != null || entity.GetComponent<NPCPlayer>() != null || entity.ShortPrefabName == "scarecrow")
                    PlayerInfos[initiator.userID].AddPoints(initiator, configData.ActionSettings.NPCKill);

                if (entity.ShortPrefabName.Contains("barrel"))
                {
                    PlayerInfos[initiator.userID].AddPoints(initiator, configData.ActionSettings.BarrelKill);
                }
            }
        }

		private void OnPlayerRespawned(BasePlayer player)
        {
            if (PlayerInfos.ContainsKey(player.userID))
            {
			    float increase = PlayerInfos[player.userID].SkillsInfo.GetCurrentIncrease("МЕТАБОЛИЗМ");
			    double Health = increase;
			    player.health += (float) Health;
			}
			return;
        }

     #endregion
     #region Interface

        private const string LayerInfo = "GUI_SystemRPG_Lvl";
        private static void UI_DrawCurrentInfo(BasePlayer player, bool fadeIn = false)
        {
            PlayerInfo playerInfo = PlayerInfos[player.userID];
            CuiElementContainer container = new CuiElementContainer();
            float percents = (float)(playerInfo.CurrentPoints - Math.Floor(playerInfo.CurrentPoints));

            CuiHelper.DestroyUi(player, LayerInfo + ".CE");
            CuiHelper.DestroyUi(player, LayerInfo + ".CL");

            container.Add(new CuiElement
            {
                FadeOut = fadeIn ? 1f : 0f, Parent = LayerInfo, Name = LayerInfo + ".CE",
                Components =
                {
                    new CuiImageComponent { Color = HexToRustFormat("#A7330DE6") },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = $"1 {percents}", OffsetMin = "1 1", OffsetMax = "-1 -1" },
                }
            });
            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.458", AnchorMax = "1 0.645" },
                Text = { Text = $"{((percents) * 100).ToString("F0")}%", FontSize = 11, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter, Color = "1 1 1 0.6" }
            }, LayerInfo, LayerInfo + ".CL");

			CuiHelper.AddUi(player, container);
        }

        public const string MenuLayer = "XMenu";
        public const string MenuItemsLayer = "XMenu.MenuItems";
        public const string MenuSubItemsLayer = "XMenu.MenuSubItems";
        public const string MenuContent = "XMenu.Content";

        private void RenderRpg(ulong userID, object[] objects)
        {
            CuiElementContainer Container = (CuiElementContainer)objects[0];
            bool FullRender = (bool)objects[1];
            string Name = (string)objects[2];
            int ID = (int)objects[3];
            int Page = (int)objects[4];

            UI_DrawResearch(BasePlayer.FindByID(userID), "", null, Container);
        }

        private static void UI_DrawResearch(BasePlayer player, string update = "", ConfigData.Skill skill = null, CuiElementContainer container = null)
        {
            PlayerInfo playerInfo = PlayerInfos[player.userID];

            if (container == null)
                container = new CuiElementContainer();

            if (update != null && string.IsNullOrEmpty(update))
            {
                CuiHelper.DestroyUi(player, MenuContent);

                container.Add(new CuiElement
                {
                    Name = MenuContent,
                    Parent = MenuLayer,
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = "0 0 0 0",
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.5 0.5",
                            AnchorMax = "0.5 0.5",
                            OffsetMin = "-430 -230",
                            OffsetMax = "490 270"
                        },
                    }
                });

				container.Add(new CuiButton
				{
					RectTransform = { AnchorMin = "0.25 0.1", AnchorMax = "0.75 0.9" },
					Button = { FadeIn = 0.1f, Color = HexToRustFormat("#8E8E8E00") },
					Text = { Text = "" }
				}, MenuContent);
				
				container.Add(new CuiLabel
				{
					RectTransform = { AnchorMin = "0 0.88", AnchorMax = "0.25 0.95" },
					Text = { Text = $"<size=14><color=#ABFF01>[ ФЕРМЕР ]</color></size>", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", }
				}, MenuContent);
				
				container.Add(new CuiLabel
				{
					RectTransform = { AnchorMin = "0.75 0.88", AnchorMax = "1 0.95" },
					Text = { Text = $"<size=14><color=#ABDDFF>[ КИЛЛЕР ]</color></size>", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", }
				}, MenuContent);

				container.Add(new CuiLabel
				{
					RectTransform = { AnchorMin = "0.15 0.9", AnchorMax = "0.85 1" },
					Text = { Text = $"<size=30><b><color=#FFFFFFB3>"+ configData.OptionsBasic.Descript +"</color></b></size>", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FadeIn = 0.1f }
				}, MenuContent);

                // Logo
                container.Add(new CuiElement
                {
                    Parent = MenuContent,
                    Components =
                        {
                            new CuiRawImageComponent { Png = ins.UrlImagesLeft },
                            new CuiRectTransformComponent { AnchorMin = "0 0.1", AnchorMax = "0.25 0.9" },
                        }
                });
                container.Add(new CuiElement
                {
                    Parent = MenuContent,
                    Components =
                        {
                            new CuiRawImageComponent { Png = ins.UrlImagesRight },
                            new CuiRectTransformComponent { AnchorMin = "0.75 0.1", AnchorMax = "1 0.9" },
                        }
                });

                container.Add(new CuiButton
				{
					RectTransform = { AnchorMin = "0.25 0.30", AnchorMax = "0.75 0.70" },
					Button = { FadeIn = 0.1f, Color = HexToRustFormat("#8E8E8E2D") },
                    Text = {
						Text = $"<size=15><b>\nКОЛИЧЕСТВО ВАШИХ RPG ОЧКОВ - <size=24>[<color=#ABFF01> {Math.Floor(playerInfo.CurrentPoints).ToString("F0")} </color>]</size> шт.</b></size>\n"+
						          $"________________________________________________________________________________________________\n"+
						  $"Система прокачки навыков игрока (RPG), вы можете прокачать все предоставленные навыки, \n"+
						   $"благодаря которым развитие станет немного быстрее, интереснее и разнообразнее. \n"+
						    $"Улучшение навыков происходит за полученный опыт, который вы можете получить добывая ресурсы,\n"+
						     $"лутая бочки/ящики, убивая игроков, ботов, животных и за проведенное время играя на сервере. \n"+
						      $"Нажав на знак вопроса который есть на каждом из окон опыта вы получите \n"+
						       $"полное описание, так же информацию о прокачке и текущем вашем опыте. \n"+
							     $"Для того что бы закрыть окно с описанием опыта, нажмите на него, а для \n"+
								   $"закрытия меню прокачки нажмите на любое свободное место. \n"+
						     $"\n\n<color=#ABFF01>Вайп навыков происходит при глобальном вайпе. (Просмотреть календарь - /wipe)</color>",
					    Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 11, FadeIn = 0.1f }
				}, MenuContent);



                var skillHeight = 80f;
                var skillMargin = 20;
                 
				var currentList = configData.SkillList.Where(p => p.Value.IsSkill);
                float minPosition = 0 - currentList.Count() / 2 * skillHeight - (currentList.Count() - 1) / 2 * skillMargin;

                foreach (var check in currentList)
                {
                    var image = ins.m_FileManager.GetPng(check.Key);
                    if (!string.IsNullOrEmpty(image))

					// картинки в рамках
					container.Add(new CuiElement
                    {
						Name = MenuContent + $".{check.Key}", Parent = MenuContent,
						Components =
						{
							new CuiRawImageComponent { Png = image },
							new CuiRectTransformComponent { AnchorMin = "0.4555 0.83", AnchorMax = "0.4555 0.83", OffsetMin = $"{minPosition} -{skillHeight / 2}", OffsetMax = $"{minPosition + skillHeight} {skillHeight / 2}" }
						}
					});
					// Окна
                    container.Add(new CuiButton
                    {
                        RectTransform =
						{
							AnchorMin = "0.4555 0.83", AnchorMax = "0.4555 0.83",
							OffsetMin = $"{minPosition} -{skillHeight / 2}", OffsetMax = $"{minPosition + skillHeight} {skillHeight / 2}"
						},
                        Button = { FadeIn = 0.1f, Color = HexToRustFormat("#8E8E8E2D"), Command = $"GUI_SystemRPG description {check.Key}" },
                        Text = { Text = "" }
                    }, MenuContent, MenuContent + $".{check.Key}");

					// Текст в кнопках
                    container.Add(new CuiLabel
					{
						RectTransform = { AnchorMin = "0 0.8", AnchorMax = "1 1.006667", OffsetMax = "0 0" },
						Text = { FadeIn = 0.1f, Text = $"Уровень - [<b><size=12><color=#ABFF01> {playerInfo.SkillsInfo.Skills[check.Key].ToString()} </color></size></b>]", Font = "robotocondensed-regular.ttf", FontSize = 12, Align = TextAnchor.MiddleCenter }
					}, MenuContent + $".{check.Key}", MenuContent + $".{check.Key}.Level");  
					container.Add(new CuiLabel
					{
						RectTransform = { AnchorMin = "0 0.110", AnchorMax = "1 1.0", OffsetMax = "0 0" },
						Text = { Text = "?", FontSize = 35, Align = TextAnchor.MiddleCenter, Color = HexToRustFormat("#FFFFFF80") }
					}, MenuContent + $".{check.Key}");

					// Текст названий окон
                    container.Add(new CuiLabel
					{
						RectTransform = { AnchorMin = "0 0.05334239", AnchorMax = "1 0.3600006", OffsetMax = "0 0" },
						Text = { FadeIn = 0.1f, Text = "<b>" + check.Value.DisplayName.ToUpper() + "</b>", Font = "robotocondensed-regular.ttf", FontSize = 12, Align = TextAnchor.MiddleCenter }
					}, MenuContent + $".{check.Key}");

                    string text = "<size=13><b>ПОДНЯТЬ</b></size>";
					string colorText = "#CFE0D3FF";
					string btnText = "#719A46FF";

                    if (playerInfo.SkillsInfo.Skills[check.Key] + 1 >= check.Value.Increase.Count)
					{
						text = "<size=13><b>УЛУЧШЕНО</b></size>"; colorText = "#CFCFE0FF"; btnText = "#6B5252FF";
					}
					else
						if (playerInfo.CurrentPoints < 1)
						{
							text = "<size=13><b>НЕХВАТАЕТ</b></size>"; colorText = "#E0CFCFFF"; btnText = "#6A5151FF";
						}

					// Текст увеличить уровень
                    container.Add(new CuiButton
					{
						RectTransform = { AnchorMin = "0 -0.226", AnchorMax = "1 -0.0133", OffsetMax = "0 0" },
						Button = { FadeIn = 0.1f, Color = HexToRustFormat(btnText), Command = $"GUI_SystemRPG increase {check.Key}" },
						Text = { FadeIn = 0.1f, Text = text, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", Color = HexToRustFormat(colorText) }
					}, MenuContent + $".{check.Key}");
                    minPosition += skillHeight; minPosition += skillMargin;
                }

                currentList = configData.SkillList.Where(p => !p.Value.IsSkill);
                minPosition = 0 - currentList.Count() / 2 * skillHeight - (currentList.Count() - 1) / 2 * skillMargin;

                foreach (var check in currentList)
                {
                    var image = ins.m_FileManager.GetPng(check.Key);
                    if (!string.IsNullOrEmpty(image))

					// картинки окон нижних
					container.Add(new CuiElement
					{
						Name = MenuContent + $".{check.Key}", Parent = MenuContent,
						Components =
						{
							new CuiRawImageComponent { Png = image },
							new CuiRectTransformComponent { AnchorMin = "0.4555 0.21", AnchorMax = "0.4555 0.21", OffsetMin = $"{minPosition} -{skillHeight / 2}", OffsetMax = $"{minPosition + skillHeight} {skillHeight / 2}" }
						}
					});

					// окна нижние
                    container.Add(new CuiButton
					{
						RectTransform = { AnchorMin = "0.4555 0.21", AnchorMax = "0.4555 0.21", OffsetMin = $"{minPosition} -{skillHeight / 2}", OffsetMax = $"{minPosition + skillHeight} {skillHeight / 2}" },
						Button = { FadeIn = 0.1f, Color = HexToRustFormat("#8E8E8E2D"), Command = $"GUI_SystemRPG description {check.Key}" },
						Text = { Text = "" }
					}, MenuContent, MenuContent + $".{check.Key}");

					// текст в нижних кнопок
                    container.Add(new CuiLabel
					{
						RectTransform = { AnchorMin = "0 0.8", AnchorMax = "1 1.006667", OffsetMax = "0 0" },
						Text = { FadeIn = 0.1f, Text = $"Уровень - [<b><size=12><color=#ABFF01> {playerInfo.SkillsInfo.Skills[check.Key].ToString()} </color></size></b>]", Font = "robotocondensed-regular.ttf", FontSize = 12, Align = TextAnchor.MiddleCenter }
					}, MenuContent + $".{check.Key}", MenuContent + $".{check.Key}.Level");
					container.Add(new CuiLabel
					{
						RectTransform = { AnchorMin = "0 0.110", AnchorMax = "1 1.0", OffsetMax = "0 0" },
						Text = { Text = "?", FontSize = 35, Align = TextAnchor.MiddleCenter, Color = HexToRustFormat("#FFFFFF80") }
					}, MenuContent + $".{check.Key}");

					// текст названий нижних окон
                    container.Add(new CuiLabel
					{
						RectTransform = { AnchorMin = "0 0.05334239", AnchorMax = "1 0.3600006", OffsetMax = "0 0" },
						Text = { FadeIn = 0.1f, Text = "<b>" + check.Value.DisplayName.ToUpper() + "</b>", Font = "robotocondensed-regular.ttf", FontSize = 12, Align = TextAnchor.MiddleCenter }
					}, MenuContent + $".{check.Key}");

                    string text = "<size=13><b>ПОДНЯТЬ</b></size>";
					string colorText = "#CFE0D3FF";
					string btnText = "#719A46FF";

                    if (playerInfo.SkillsInfo.Skills[check.Key] + 1 >= check.Value.Increase.Count)
					{
						text = "<size=13><b>УЛУЧШЕНО</b></size>"; colorText = "#CFCFE0FF"; btnText = "#6B5252FF";
					}
                    else
						if (playerInfo.CurrentPoints < 1)
						{
							text = "<size=13><b>НЕХВАТАЕТ</b></size>"; colorText = "#E0CFCFFF"; btnText = "#6A5151FF";
						}

					// Нижние кнопки увилечения уровня
                    container.Add(new CuiButton
					{
						RectTransform = { AnchorMin = "0 -0.226", AnchorMax = "1 -0.0133", OffsetMax = "0 0" },
						Button = { FadeIn = 0.1f, Color = HexToRustFormat(btnText), Command = $"GUI_SystemRPG increase {check.Key}" },
						Text = { FadeIn = 0.1f, Text = text, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", Color = HexToRustFormat(colorText) }
					}, MenuContent + $".{check.Key}");
                    minPosition += skillHeight; minPosition += skillMargin;
                }
            }

            if (!string.IsNullOrEmpty(update))
            {
                if (!update.Contains("ERROR") && !update.Contains("UPER"))
                {
                    CuiHelper.DestroyUi(player, MenuContent + $".{update}.Level");

                    container.Add(new CuiLabel
                    {
                        RectTransform = { AnchorMin = "0 0.8", AnchorMax = "1 1.006667", OffsetMax = "0 0" },
                        Text =
						{
							FadeIn = 0.1f,
							Text = $"[<b>Уровень - <size=12><color=#ABFF01> {playerInfo.SkillsInfo.Skills[update].ToString()} </color></size></b>]",
							Font = "robotocondensed-regular.ttf", FontSize = 12, Align = TextAnchor.MiddleCenter
						}
                    }, MenuContent + $".{update}", MenuContent + $".{update}.Level");
					container.Add(new CuiLabel
                    {
                        RectTransform = { AnchorMin = "0 0.110", AnchorMax = "1 1.0", OffsetMax = "0 0" },
                        Text = { Text = "?", FontSize = 35, Align = TextAnchor.MiddleCenter, Color = HexToRustFormat("#FFFFFF80") }
                    }, MenuContent + $".{update}");
                    UI_DrawCurrentInfo(player);
                }
				else
					if (update.Contains("ERROR"))
                    {
                        CuiHelper.DestroyUi(player, MenuContent + $".{update}.Level");
				    
                        container.Add(new CuiButton
                        {
                            RectTransform = { AnchorMin = "0 -0.226", AnchorMax = "1 -0.0133", OffsetMax = "0 0" },
                            Button = { FadeIn = 0.1f, Color = HexToRustFormat("#6A5151FF"), Command = $"GUI_SystemRPG increase {update.Replace("ERROR", "")}" },
                            Text = { FadeIn = 0.1f, Text = "<size=13><b>НЕХВАТАЕТ</b></size>", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", Color = HexToRustFormat("#E0CFCFFF") }
                        }, MenuContent + $".{update.Replace("ERROR", "")}", MenuContent + $".{update.Replace("ERROR", "")}.Upgrade");
                    }
                // обновление максимального окна
				else
					if (update.Contains("UPER"))
                    {
                        CuiHelper.DestroyUi(player, MenuContent + $".{update}.Level");
				    
                        container.Add(new CuiButton
                        {
				    		RectTransform = { AnchorMin = "0 -0.226", AnchorMax = "1 -0.0133", OffsetMax = "0 0" },
				    		Button = { FadeIn = 0.1f, Color = HexToRustFormat("#6B5252FF"), Command = $"GUI_SystemRPG increase {update.Replace("UPER", "")}" },
                            Text = { FadeIn = 0.1f, Text = "<size=13><b>УЛУЧШЕНО</b></size>", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", Color = HexToRustFormat("#CFCFE0FF") }
                        }, MenuContent + $".{update.Replace("UPER", "")}", MenuContent + $".{update.Replace("UPER", "")}.Upgrade");
                    }
                CuiHelper.AddUi(player, container);
            }

            if (skill != null)
            {
                CuiHelper.DestroyUi(player, MenuContent + ".Description");

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.25 0.30", AnchorMax = "0.75 0.70" },
                    Button = { FadeIn = 0.1f, Color = "0.29 0.29 0.27 0.95", Material = "assets/content/ui/uibackgroundblur.mat", Close = MenuContent + ".Description" },
					Text =
					{
						FadeIn = 0.1f,
						Text = skill.Description,
						Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 12
					}
                }, MenuContent, MenuContent + ".Description");
				container.Add(new CuiLabel
				{
					RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0.3" },
					Text = { FadeIn = 1f, Text = $"--------------------------\nВаш текущий уровень: <color=#AAF415><b>{playerInfo.SkillsInfo.Skills[skill.DisplayName]}</b></color>"+ $"\nВаш текущий рейт способности: <color=#AAF415>"+ $"<b>{(skill.DisplayName != "ВАМПИРИЗМ" ? playerInfo.SkillsInfo.GetCurrentIncrease(skill.DisplayName) : playerInfo.SkillsInfo.GetCurrentIncrease(skill.DisplayName))}</b></color>", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 12 }
				}, MenuContent + ".Description");
                CuiHelper.AddUi(player, container);
            }
            UI_DrawCurrentInfo(player);
        }

     #endregion
     #region Commands

        [ConsoleCommand("system.points")]
        private void CmdAdminHandler(ConsoleSystem.Arg args)
        {
            if (args.Player() != null || !args.HasArgs(2))
                return;

            ulong targetID;
            int amount;
            if (ulong.TryParse(args.Args[0], out targetID))
            {
                BasePlayer target = BasePlayer.FindByID(targetID);
                if (int.TryParse(args.Args[1], out amount))
                {
                    if (PlayerInfos.ContainsKey(targetID))
                    {
                        PlayerInfos[targetID].AddPoints(target, amount);
                        PrintWarning($"Successful added {amount} to {targetID}");

                        if (target != null && target.IsConnected)
                            target.ChatMessage($"Вы получили {amount} очков, вы можете потратить их в меню!");
                    }
                }
            }
        }

        [ChatCommand("rpg")]
        private void CmdChatCommand(BasePlayer player, string command, string[] args)
        {
            player.SendConsoleCommand("GUI_SystemRPG open");
        }

        [ConsoleCommand("GUI_SystemRPG")]
        private void CmdConsoleHandler(ConsoleSystem.Arg args)
        {
            BasePlayer player = args.Player();
            if (player != null && args.HasArgs(1))
            {
                switch (args.Args[0].ToLower())
                {
                    case "increase":
                    {
                        if (args.HasArgs(2) && configData.SkillList.ContainsKey(args.Args[1]))
                        {
                            PlayerInfos[player.userID].AddLevel(player, args.Args[1]);
                        }
                        break;
                    }
                    case "open":
                    {
                        UI_DrawResearch(player);
                        break;
                    }
                    case "description":
                    {
                        if (args.HasArgs(2))
                        {
                            string codeName = args.Args[1];
                            if (configData.SkillList.ContainsKey(codeName))
                            {
                                UI_DrawResearch(player, null, configData.SkillList[codeName]);
                                return;
                            }
                        }

                        break;
                    }
                }
            }
        }

     #endregion
     #region Utils       

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

            UnityEngine.Color color = new Color32(r, g, b, a);
            return string.Format("{0:F2} {1:F2} {2:F2} {3:F2}", color.r, color.g, color.b, color.a);
        }

     #endregion
     #region LoadImages

		bool init;
        private GameObject FileManagerObject;
        private FileManager m_FileManager;
        private string UrlImagesLeft;
        private string UrlImagesRight;

		IEnumerator LoadImages()
        {
            if (!string.IsNullOrEmpty(configData.OptionsBasic.UrlImgLeft))
            {
                UrlImagesLeft = configData.OptionsBasic.UrlImgLeft;
                yield return m_FileManager.StartCoroutine(m_FileManager.LoadFile(UrlImagesLeft, UrlImagesLeft));
                UrlImagesLeft = m_FileManager.GetPng(UrlImagesLeft);
            }
			//else
            //{
            //    UrlImagesLeft = "https://imgur.com/BdDbsOW.png";
            //    yield return m_FileManager.StartCoroutine(m_FileManager.LoadFile(UrlImagesLeft, UrlImagesLeft));
            //    UrlImagesLeft = m_FileManager.GetPng(UrlImagesLeft);
            //}

			if (!string.IsNullOrEmpty(configData.OptionsBasic.UrlImgRight))
            {
                UrlImagesRight = configData.OptionsBasic.UrlImgRight;
                yield return m_FileManager.StartCoroutine(m_FileManager.LoadFile(UrlImagesRight, UrlImagesRight));
                UrlImagesRight = m_FileManager.GetPng(UrlImagesRight);
            }
			//else
            //{
            //    UrlImagesRight = "https://imgur.com/PsopjNT.png";
            //    yield return m_FileManager.StartCoroutine(m_FileManager.LoadFile(UrlImagesRight, UrlImagesRight));
            //    UrlImagesRight = m_FileManager.GetPng(UrlImagesRight);
            //}
        }

        void InitFileManager()
        {
            FileManagerObject = new GameObject("FileManagerObject");
            m_FileManager = FileManagerObject.AddComponent<FileManager>();
        }

        class FileManager : MonoBehaviour
        {
            int loaded = 0;
            int needed = 0;
            public bool IsFinished => needed == loaded;
            const ulong MaxActiveLoads = 10;
            Dictionary<string, FileInfo> files = new Dictionary<string, FileInfo>();

            private class FileInfo
            {
                public string Url;
                public string Png;
            }

            public string GetPng(string name)
            {
                if (files.ContainsKey(name))
                    return files[name].Png;
                return null;
            }

            public IEnumerator LoadFile(string name, string url, int size = -1)
            {
                if (files.ContainsKey(name) && files[name].Url == url && !string.IsNullOrEmpty(files[name].Png)) yield break;
                files[name] = new FileInfo() { Url = url };
                needed++;
                yield return StartCoroutine(LoadImageCoroutine(name, url, size));
            }
            IEnumerator LoadImageCoroutine(string name, string url, int size = -1)
            {
                using (WWW www = new WWW(url))
                {
                    yield return www;
                    if (string.IsNullOrEmpty(www.error))
                    {
                        var bytes = size == -1 ? www.bytes : Resize(www.bytes, size);
                        var entityId = CommunityEntity.ServerInstance.net.ID;
                        var crc32 = FileStorage.server.Store(bytes, FileStorage.Type.png, entityId).ToString();
                        files[name].Png = crc32;
                    }

                }
                loaded++;
                ins.init = true;
            }
            static byte[] Resize(byte[] bytes, int size)
            {
                Image img = (Bitmap)(new ImageConverter().ConvertFrom(bytes));
                Bitmap cutPiece = new Bitmap(size, size);
                System.Drawing.Graphics graphic = System.Drawing.Graphics.FromImage(cutPiece);
                graphic.DrawImage(img, new Rectangle(0, 0, size, size), 0, 0, img.Width, img.Height, GraphicsUnit.Pixel);
                graphic.Dispose();
                MemoryStream ms = new MemoryStream();
                cutPiece.Save(ms, ImageFormat.Jpeg);
                return ms.ToArray();
            }
        }
     #endregion
    }
}