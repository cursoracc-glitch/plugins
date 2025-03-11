using System;
using Oxide.Core.Plugins;
using UnityEngine;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Game.Rust.Libraries;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("MachiningTools", "Vlad-00003", "1.0.7", ResourceId = 89)]
    [Description("Allow admins to give enchanted items to the players, wich would gaver processed items")]
    /*
     * Author info:
     *   E-mail: Vlad-00003@mail.ru
     *   Vk: vk.com/vlad_00003
     */
    class MachiningTools : RustPlugin
    {
        #region Vars
        private Dictionary<uint, SavedData> Tools;
        private PluginConfig config;
        private Dictionary<ItemDefinition, ItemDefinition> Transmutations;
        private List<string> Transmutatable = new List<string>()
        {
            "chicken.raw",
            "humanmeat.raw",
            "bearmeat",
            "deermeat.raw",
            "meat.boar",
            "wolfmeat.raw",
            "hq.metal.ore",
            "metal.ore",
            "sulfur.ore"
        };
        #endregion

        #region Data handling
        private class SavedData
        {
            public Transmutetion transmuatation;
            public bool CanRepair;
            public bool CanRecycle;
        }
        private void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject(Title, Tools);
        }
        void LoadData()
        {
            try
            {
                Tools = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<uint, SavedData>>(Title);
            }
            catch (Exception ex)
            {
                PrintError($"Failed to load cupboard data file (is the file corrupt?) ({ex.Message})");
                Tools = new Dictionary<uint, SavedData>();
            }
        }
        #endregion

        #region Config
        private class Tool
        {
            [JsonProperty("Короткое имя предмета")]
            public string Item;
            [JsonProperty("ID скина предмета (Поддерживается Workshop)")]
            public ulong SkinID;
            [JsonProperty("Можно ли ремонтировать предмет")]
            public bool CanRepair;
            [JsonProperty("Можно ли перерабатывать пердмет")]
            public bool CanRecycle;
            [JsonProperty("Настройки переработки")]
            public Transmutetion transmutation;
        }
        private class Transmutetion
        {
            [JsonProperty("Перерабатывать дерево в уголь")]
            public bool Wood;
            [JsonProperty("Перерабатывать руду МВК в металл")]
            public bool HQM;
            [JsonProperty("Перерабатывать металлическую руду в фрагменты")]
            public bool Metal;
            [JsonProperty("Перерабатывать серную руду в серу")]
            public bool Sulfur;
            [JsonProperty("Перерабатывать мясо медведя в жаренное")]
            public bool Bear;
            [JsonProperty("Перерабатывать свинину в жаренную")]
            public bool Boar;
            [JsonProperty("Перерабатывать мясо курицы в жаренное")]
            public bool Chicken;
            [JsonProperty("Перерабатывать мясо волка в жаренное")]
            public bool Wolf;
            [JsonProperty("Перерабатывать мясо оленя в жаренное")]
            public bool Deer;
            [JsonProperty("Перерабатывать человеческое мясо в жаренное")]
            public bool Human;
            public static Transmutetion DefaultPick()
            {
                return new Transmutetion()
                {
                    Wood = false,
                    HQM = true,
                    Metal = true,
                    Sulfur = true,
                    Bear = false,
                    Boar = false,
                    Chicken = false,
                    Wolf = false,
                    Deer = false,
                    Human = false
                };
            }
            public static Transmutetion DefaultAxe()
            {
                return new Transmutetion()
                {
                    Wood = true,
                    HQM = false,
                    Metal = false,
                    Sulfur = false,
                    Bear = true,
                    Boar = true,
                    Chicken = true,
                    Wolf = true,
                    Deer = true,
                    Human = true
                };
            }
        }
        private class PluginConfig
        {
            [JsonProperty("Привилегия для использования команд")]
            public string Permission;
            [JsonProperty("Команда(чат/консоль)")]
            public string Command;
            [JsonProperty("Список инструментов")]
            public Dictionary<string, Tool> Tools;
            public static PluginConfig DefaultConfig()
            {
                return new PluginConfig()
                {
                    Permission = "machiningtools.use",
                    Command = "givetool",
                    Tools = new Dictionary<string, Tool>()
                    {
                        ["hatchet"] = new Tool()
                        {
                            Item = "hatchet",
                            CanRepair = true,
                            CanRecycle = true,
                            SkinID = 901876821,
                            transmutation = Transmutetion.DefaultAxe()
                        },
                        ["pickaxe"] = new Tool()
                        {
                            Item = "pickaxe",
                            CanRepair = true,
                            CanRecycle = true,
                            SkinID = 902892485,
                            transmutation = Transmutetion.DefaultPick()
                        },
                        ["icepick"] = new Tool()
                        {
                            Item = "icepick.salvaged",
                            CanRepair = false,
                            CanRecycle = false,
                            SkinID = 804307574,
                            transmutation = Transmutetion.DefaultPick()
                        },
                        ["axe"] = new Tool()
                        {
                            Item = "axe.salvaged",
                            CanRepair = false,
                            CanRecycle = false,
                            SkinID = 0,
                            transmutation = Transmutetion.DefaultAxe()
                        }
                    }
                };
            }
        }
        #endregion

        #region Config handling
        protected override void LoadDefaultConfig()
        {
            PrintWarning("Благодарим за приобритение плагина на сайте RustPlugin.ru. Если вы приобрели этот плагин на другом ресурсе знайте - это лишает вас гарантированных обновлений!");
            config = PluginConfig.DefaultConfig();
        }
        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<PluginConfig>();
        }
        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }
        #endregion

        #region Init and quiting
        void Init()
        {
            LoadData();
            AddCovalenceCommand(config.Command, "GiveToolsCommand", config.Permission);
        }
        void OnServerInitialized()
        {
            Transmutations = ItemManager.GetItemDefinitions().Where(p => Transmutatable.Contains(p.shortname))
                .ToDictionary(p => p, p => p.GetComponent<ItemModCookable>()?.becomeOnCooked);
            ItemDefinition wood = ItemManager.FindItemDefinition(-151838493);
            ItemDefinition charcoal = ItemManager.FindItemDefinition(-1938052175);
            Transmutations.Add(wood, charcoal);
            foreach (var item in Transmutations)
            {
                if (item.Value == null)
                {
                    PrintError($"Не удалось получить ItemModCookable для \"{item.Key.displayName.english}\"\nСообщите об этом разработчику: https://vk.com/vlad_00003");
                }
            }
        }
        void Unload() => SaveData();
        void OnServerSave() => SaveData();
        #endregion

        #region Oxide Hooks
        void OnNewSave(string filename)
        {
            Tools.Clear();
            PrintWarning("Обнаружен вайп. Инструменты сброшены.");
        }
        void OnEntityKill(BaseNetworkable entity)
        {
			if(entity?.net?.ID == null) return;
            if (Tools.ContainsKey(entity.net.ID))
                Tools.Remove(entity.net.ID);
        }
        object OnItemRepair(BasePlayer player, Item item)
        {
            var entity = item.GetHeldEntity()?.net.ID;
            if (entity.HasValue)
            {
                SavedData data;
                if(Tools.TryGetValue(entity.Value, out data))
                {
                    if(!data.CanRepair)
                    {
                        player.ChatMessage(GetMsg("Can't repair", player.userID));
                        return false;
                    }
                }
            }
            return null;
        }
        object CanRecycle(Recycler recycler, Item item)
        {
            var entity = item.GetHeldEntity()?.net.ID;
            if (entity.HasValue)
            {
                SavedData data;
                if (Tools.TryGetValue(entity.Value, out data))
                {
                    if (!data.CanRecycle)
                    {
                        return false;
                    }
                }
            }
            return null;
        }
        void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            HeldEntity weapon = entity.ToPlayer()?.GetHeldEntity();
            if (weapon == null) return;
            SavedData data;
            if (!Tools.TryGetValue(weapon.net.ID, out data)) return;
			
            switch (item.info.shortname)
            {
                case "stones":
                    break;
                case "leather":
                    break;
                case "bone.fragments":
                    break;
                case "fat.animal":
                    break;
                case "skull.wolf":
                    break;
                case "cloth":
                    break;
                case "cactusflesh":
                    break;
                case "skull.human":
                    break;
                case "humanmeat.raw":
                    if (data.transmuatation.Human)
                        Transmutate(item);
                    break;
                case "bearmeat":
                    if (data.transmuatation.Bear)
                        Transmutate(item);
                    break;
                case "chicken.raw":
                    if (data.transmuatation.Chicken)
                        Transmutate(item);
                    break;
                case "meat.boar":
                    if (data.transmuatation.Boar)
                        Transmutate(item);
                    break;
                case "deermeat.raw":
                    if (data.transmuatation.Deer)
                        Transmutate(item);
                    break;
                case "wolfmeat.raw":
                    if (data.transmuatation.Wolf)
                        Transmutate(item);
                    break;
                case "sulfur.ore":
                    if (data.transmuatation.Sulfur)
                        Transmutate(item);
                    break;
                case "metal.ore":
                    if (data.transmuatation.Metal)
                        Transmutate(item);
                    break;
                case "wood":
                    if (data.transmuatation.Wood)
                        Transmutate(item);
                    break;
                default:
                    Puts($"Игрок добыл неизвестный предмет - {item.info}!\nСообщите об этом разработчику: https://vk.com/vlad_00003");
                    break;
            }
        }
		
        void OnDispenserBonus(ResourceDispenser disp, BasePlayer player, Item item)
        {
            if (player == null)
            {
                Puts("Финальный бонус был присвоен без игрока!\nСообщите об этом разработчику: https://vk.com/vlad_00003");
                return;
            }
            var weapon = player.GetHeldEntity();
            if (weapon == null) return;
            SavedData data;
            if (!Tools.TryGetValue(weapon.net.ID, out data)) return;
            switch (item.info.shortname)
            {
                case "sulfur.ore":
                    if (data.transmuatation.Sulfur)
                        Transmutate(item);
                    break;
                case "metal.ore":
                    if (data.transmuatation.Metal)
                        Transmutate(item);
                    break;
                case "hq.metal.ore":
                    if (data.transmuatation.HQM)
                        Transmutate(item);
                    break;
                case "stones":
                    break;
                case "wood":
                    if (data.transmuatation.Wood)
                        Transmutate(item);
                    break;
                default:
                    Puts($"Игроку присвоен неизвестный предмет - {item.info.shortname}!\nСообщите об этом разработчику: https://vk.com/vlad_00003");
                    break;
            }
        }
        #endregion
        
        #region Localization
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>()
            {
                ["Syntax"] = "Incorrect synax! Use: {0} player item [item2] [item3] ...",
                ["No item"] = "Item \"{0}\" could not found in the tool list and can't be given!",
                ["No player"] = "Player \"{0}\" could not be found",
                ["Not on server"] = "Player \"{0}\" is not on the server",
                ["Multiply players"] = "Found multiply players:\n{0}",
                ["Successfull"] = "Successfully gave player \"{0}\" tools:\n{1}",
                ["Can't repair"] = "You can not repair this tool!"
            }, this);
            lang.RegisterMessages(new Dictionary<string, string>()
            {
                ["Syntax"] = "Неверный синтаксис! Используйте: {0} player item [item2] [item3] ...",
                ["No item"] = "Предмет \"{0}\" не найден в списке инструментов и не может быть выдан!",
                ["No player"] = "Игрок \"{0}\" не найден",
                ["Not on server"] = "Игрок \"{0}\" не находится на сервере",
                ["Multiply players"] = "Найдено несколько игроков:\n{0}",
                ["Successfull"] = "Успешно выдали игроку \"{0}\" предметы:\n{1}",
                ["Can't repair"] = "Данный предмет не подлежит ремонту!"
            }, this, "ru");
        }
        private string GetMsg(string langkey, object userID = null) => lang.GetMessage(langkey, this, userID == null ? null : userID.ToString());
        #endregion

        #region Command
        private void GiveToolsCommand(IPlayer player, string command, string[] args)
        {
            if(args.Length < 2)
            {
                Reply(player, "Syntax", config.Command);
                return;
            }
            var recivers = GetPlayers(args[0]);
            if (recivers == null || recivers.Count == 0)
            {
                Reply(player, "No player", args[0]);
                return;
            }
            if(recivers.Count > 1)
            {
                Reply(player, "Multiply players", string.Join("\n", recivers.Select(p => $"{p.Value} ({p.Key})").ToArray()));
                return;
            }
            var Ireciver = recivers.First();
            var reciver = FindBasePlayer(Ireciver.Key);
            if (reciver == null)
            {
                Reply(player, "Not on server", Ireciver.Value);
                return;
            }
            var tools = args.ToList();
            tools.RemoveAt(0);
            var ToolCheck = tools.Where(t => !config.Tools.ContainsKey(t));
            foreach(var mistake in ToolCheck)
            {
                Reply(player, "No item", mistake);
            }
            if (ToolCheck.Count() > 0) return;
            foreach(var tool in tools)
            {
                var data = config.Tools[tool];
                Item item = ItemManager.CreateByName(data.Item, 1, data.SkinID);
                reciver.GiveItem(item);
                uint id = item.GetHeldEntity().net.ID;
                Tools.Add(id, new SavedData()
                {
                    CanRepair = data.CanRepair,
                    transmuatation = data.transmutation,
                    CanRecycle = data.CanRecycle
                });
            }
            Reply(player, "Successfull", Ireciver.Value, string.Join("\n", tools.ToArray()));
        }
        #endregion

        #region API
        object IsMachiningToolEnt(BaseEntity entity)
        {
            if (entity?.net?.ID == null)
                return null;
            return Tools.ContainsKey(entity.net.ID);
        }
        object IsMachiningToolItem(Item item)
        {
            var entity = item.GetHeldEntity();
            if (entity == null) return null;
            if (entity?.net?.ID == null)
                return null;
            return Tools.ContainsKey(entity.net.ID);
        }
        #endregion

        #region Helpers
        private void Transmutate(Item item)
        {
            if (!Transmutations.ContainsKey(item.info))
            {
                PrintWarning($"Неизвестный предмет отправлен на переплавку - {item.info.displayName.english}!\nСообщите об этом разработчику: https://vk.com/vlad_00003");
                return;
            }
            item.info = Transmutations[item.info];
        }
        private void Reply(IPlayer player, string langkey, params object[] args)
        {
            player.Reply(string.Format(GetMsg(langkey, player.Id), args));
        }
        private Dictionary<ulong, string> GetPlayers(string NameOrID)
        {
            var pl = covalence.Players.FindPlayers(NameOrID).ToList();
            return pl.Select(p => new KeyValuePair<ulong, string>(ulong.Parse(p.Id), p.Name)).ToDictionary(x => x.Key, x => x.Value);
        }
        private BasePlayer FindBasePlayer(ulong userID)
        {
            BasePlayer player = BasePlayer.activePlayerList.Where(p => p.userID == userID).FirstOrDefault();
            player = player == null ? BasePlayer.sleepingPlayerList.Where(p => p.userID == userID).FirstOrDefault() : player;
            return player;
        }
        #endregion
    }
}