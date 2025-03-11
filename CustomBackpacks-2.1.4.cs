using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core;
using UnityEngine;
using Array = System.Array;

namespace Oxide.Plugins;

[Info("CustomBackpacks", "misty.dev (Cobalt Studios)", "2.1.4")]
[Description("Allows you to create custom backpacks and add them to the loot tables.")]
public class CustomBackpacks : RustPlugin
{
    #region Private Fields

    private Configuration _config;
    private Dictionary<ulong, Item> _backpacks = new();
    private static VersionNumber _configVersion = new(2, 1, 4);
    private const GameTip.Styles Error = GameTip.Styles.Error;

    #endregion

    #region Hooks

    private void Init()
    {

        _config.Commands.TryAdd("give", "cb.give");
        
        cmd.AddChatCommand(_config.Commands["give"], this, nameof(GivePlayerBackpack));
        cmd.AddConsoleCommand(_config.Commands["give"], this, nameof(ConsoleGivePlayerBackpack));
        permission.RegisterPermission("custombackpacks.give", this);
        
        if(!_config.RemoveDefaultBackpacks) Unsubscribe("OnLootSpawn");
    }
    
    private object OnBackpackDrop(Item backpack, PlayerInventory inv)
    {
        var player = inv._baseEntity;
        
        if (player == null || player.health > 0) return null!;
        
        if (!_config.Backpacks.TryGetValue(backpack.name, out BackpackInfo backpackInfo)) return null!;
        
        if (!backpackInfo.SaveContentsOnDeath) return null!;
        
        _backpacks.Add(player.userID.Get(), backpack);
        
        backpack.RemoveFromContainer();
        backpack.RemoveFromWorld();

        return null!;
    }

    private void OnPlayerRespawn(BasePlayer player, BasePlayer.SpawnPoint _)
    {
        var playerId = player.userID.Get();
        timer.Once(0.2f, () =>
        {
            GiveBackpack(player);
            _backpacks.Remove(playerId);
        });
    }

    private void OnLootSpawn(LootContainer container)
    {
        if (_config.RemoveDefaultBackpacks) RemoveBackpacks(container);

        foreach (var kvp in _config.LootSpawns)
        {
            if(kvp.Key != container.PrefabName) continue;

            foreach (var backpack in kvp.Value)
            {
                var rndm = UnityEngine.Random.Range(0f, 100.1f);

                if (!_config.Backpacks.TryGetValue(backpack.Key, out BackpackInfo info)) continue;

                if (!(backpack.Value > rndm)) continue;

                container.inventory.capacity++;
                container.inventory.GiveItem(GetBackpack(info.Shortname, info.Capacity, backpack.Key));
            }
        }
    }

    #endregion

    #region Commands
    private void ConsoleGivePlayerBackpack(ConsoleSystem.Arg arg) => GivePlayerBackpack(arg.Player(), null!, arg.Args);

    private void GivePlayerBackpack(BasePlayer player, string command, string[] args)
    {
        BasePlayer target;
        string name;
        if (player == null)
        {
            if (args.IsEmpty() || args.Length < 2)
            {
                SendMessage(null, "InvalidUsage", Error, _config.Commands["give"]);
                return;
            }

            name = args[0];

            if (!_config.Backpacks.TryGetValue(name, out BackpackInfo backpackInfo))
            {
                SendMessage(null, "InvalidBackpack", Error);
                return;
            }

            target = BasePlayer.Find(args[1]);

            if (target == null)
            {
                SendMessage(null, "InvalidTarget", Error);
                return;
            }

            GiveBackpack(target, name);

            SendMessage(null, "GaveBackpack", args: new object[] { target.displayName, name });
            return;
        }
        
        if (!permission.UserHasPermission(player.UserIDString, "custombackpacks.give"))
        {
            SendMessage(player, "MissingPermission", Error);
            return;
        }

        if (args.IsEmpty() || args.Length < 2)
        {
            // SendError(player, "Invalid usage, /cb.give <backpack> <steamIdOrName>");
            SendMessage(player, "InvalidUsage", Error, _config.Commands["give"]);
            return;
        }

        name = args[0];

        if (!_config.Backpacks.TryGetValue(name, out BackpackInfo backpack))
        {
            SendMessage(player, "InvalidBackpack", Error);
            return;
        }

        target = BasePlayer.Find(args[1]);

        if (target == null)
        {
            // SendError(player, "Invalid steam id or name provided.");
            SendMessage(player, "InvalidTarget", Error);
            return;
        }

        GiveBackpack(target, name);

        // player.ShowToast(GameTip.Styles.Blue_Normal, new Translate.Phrase(eng: $"Gave {target.displayName} {name}"),
        //     false, Array.Empty<string>());
        SendMessage(player, "GaveBackpack", args: new object[] { target.displayName, name });
    }

    #endregion

    #region Helper Functions
    private void RemoveBackpacks(LootContainer container)
    {
        for (var index = container.inventory.itemList.Count - 1; index >= 0; index--)
        {
            var backpack = container.inventory.itemList[index];

            if (backpack.info.itemid is -907422733 or 2068884361)
            {
                backpack.Remove();
            }
        }
    }

    private Item GetBackpack(string shortname, int capacity, string name)
    {
        var item = ItemManager.CreateByName(shortname);

        if (!item.IsBackpack()) return item;
        
        item.contents.capacity = capacity;
        item.name = name;

        item.contents.canAcceptItem = CanAccept;

        return item;
    }

    private void GiveBackpack(BasePlayer player, string? name = null)
    {
        var playerId = player.userID.Get();
        if (!_backpacks.TryGetValue(playerId, out Item backpack))
        {
            // If name is null, we know its when we are doing so on respawn.
            if (name == null) return;
            
            if (!_config.Backpacks.TryGetValue(name, out BackpackInfo bp)) return;
            backpack = GetBackpack(bp.Shortname, bp.Capacity, name);
        }

        if (player.inventory.containerWear.GetSlot(ItemContainer.BackpackSlotIndex) != null)
        {
            player.inventory.GiveItem(backpack);
            return;
        }

        backpack.MoveToContainer(player.inventory.containerWear, ItemContainer.BackpackSlotIndex);
    }

    private bool CanAccept(Item item, int _)
    {
        var player = item.GetOwnerPlayer();

        if (player == null) return true;

        var backpack = player.inventory.GetBackpackWithInventory();

        if (backpack == null) return true;

        if (!_config.Backpacks.TryGetValue(backpack.name, out BackpackInfo info)) return true;
        
        if (info.ItemBlackList.Contains(item.info.shortname))
        {
            // SendError(player, $"{backpack.name} cannot accept {item.info.displayName.translated}");
            SendMessage(player, "CannotAcceptItem", GameTip.Styles.Error, backpack.name,
                item.info.displayName.translated);
            return false;
        };
        
        return true;
    }

    #endregion

    #region Localization

    protected override void LoadDefaultMessages()
    {
        lang.RegisterMessages(new Dictionary<string, string>
        {
            ["CannotAcceptItem"] = "{0} cannot accept {1}",
            ["InvalidTarget"] = "Invalid steam id or name provided.",
            ["InvalidBackpack"] = "Invalid backpack name provided.",
            ["MissingPermission"] = "Missing permissions to run this command",
            ["GaveBackpack"] = "Gave {0} {1}",
            ["InvalidUsage"] = "Invalid usage, {0} <backpack> <steamIdOrName>"
        }, this);
    }
    
    private void SendMessage(BasePlayer? player, string key, GameTip.Styles style = GameTip.Styles.Blue_Normal, params object[] args)
    {
        var message = string.Format(lang.GetMessage(key, this, player?.UserIDString), args);
        if (player == null)
        {
            Puts(message);
            return;
        }
        Translate.Phrase phrase =
            new Translate.Phrase(eng: message);

        player.ShowToast(style, phrase, false, Array.Empty<string>());
    }

    #endregion
    
    #region Configuration

    private class Configuration
    {
        [JsonProperty("RemoveDefaultBackpacks", ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public bool RemoveDefaultBackpacks = false;
        [JsonProperty("Backpacks", ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public Dictionary<string, BackpackInfo> Backpacks = new Dictionary<string, BackpackInfo>
        {
            ["rucksack"] = new BackpackInfo()
            {
                Shortname = "largebackpack",
                SaveContentsOnDeath = true,
                Capacity = 8,
                ItemBlackList = new List<string>
                {
                    "rifle.ak",
                    "sulfur.ore",
                    "lmg.m249"
                },
            }
        };

        [JsonProperty("Command Names", ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public Dictionary<string, string> Commands = new Dictionary<string, string>
        {
            ["give"] = "cb.give"
        };

        [JsonProperty("LootSpawns", ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public Dictionary<string, Dictionary<string, float>> LootSpawns =
            new Dictionary<string, Dictionary<string, float>>
            {
                ["assets/bundled/prefabs/radtown/crate_basic.prefab"] = new Dictionary<string, float>
                {
                    ["rucksack"] = 100f
                }
            };

        [JsonProperty("Version")] public VersionNumber VersionNumber = _configVersion;
    }
    
    private class BackpackInfo
    {
        [JsonProperty("Shortname")] public string Shortname;
        [JsonProperty("SaveContentsOnDeath")] public bool SaveContentsOnDeath;
        [JsonProperty("Capacity")] public int Capacity;
        [JsonProperty("ItemBlackList")] public List<string> ItemBlackList;
    }

    protected override void LoadDefaultConfig() => _config = new Configuration();

    protected override void LoadConfig()
    {
        base.LoadConfig();

        try
        {
            _config = Config.ReadObject<Configuration>();

            if (_config != null) return;

            if (_config.VersionNumber >= Version) return;

            Puts("Config is outdated, updating config...");
            UpdateConfig();
            SaveConfig();
        }
        catch
        {
            LoadDefaultConfig();
            SaveConfig();
        }
    }

    protected override void SaveConfig() => Config.WriteObject(_config, true);

    private void UpdateConfig()
    {
        _config.VersionNumber = Version;
    }
    #endregion
} 