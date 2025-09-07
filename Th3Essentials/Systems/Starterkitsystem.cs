using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Th3Essentials.Config;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common;
using Vintagestory.Server;

namespace Th3Essentials.Systems;

internal class Starterkitsystem
{
    private Th3Config _config = null!;

    private Th3PlayerConfig _playerConfig = null!;
    private ICoreServerAPI _sapi = null!;

    internal void Init(ICoreServerAPI sapi)
    {
        _config = Th3Essentials.Config;
        _playerConfig = Th3Essentials.PlayerConfig;
        _sapi = sapi;
        RegisterCommands(sapi);
    }

    private void RegisterCommands(ICoreServerAPI sapi)
    {
        sapi.ChatCommands.Create("starterkit")
            .WithDescription(Lang.Get("th3essentials:cd-starterkit"))
            .RequiresPlayer()
            .RequiresPrivilege(Privilege.chat)
            .HandleWith(args => TryGiveItemStack(sapi, (IServerPlayer)args.Caller.Player));
            
        sapi.ChatCommands.Create("setstarterkit")
            .WithDescription(Lang.Get("th3essentials:cd-setstarterkit"))
            .RequiresPlayer()
            .RequiresPrivilege(Privilege.controlserver)
            .HandleWith(OnSetStarterKit);
            
        sapi.ChatCommands.Create("resetstarterkitusageall")
            .WithDescription(Lang.Get("th3essentials:cd-rstall"))
            .RequiresPrivilege(Privilege.controlserver)
            .WithArgs(sapi.ChatCommands.Parsers.OptionalWord("confirm"))
            .HandleWith(OnResetAllKits);

        sapi.ChatCommands.Create("resetstarterkitusage")
            .WithDescription(Lang.Get("th3essentials:cd-rstp"))
            .RequiresPlayer()
            .RequiresPrivilege(Privilege.controlserver)
            .WithArgs(sapi.ChatCommands.Parsers.OnlinePlayer("player"))
            .HandleWith(OnResetKit);

        sapi.ChatCommands.Create("setstarterkitusage")
            .WithDescription(Lang.Get("th3essentials:cd-rstp"))
            .RequiresPlayer()
            .RequiresPrivilege(Privilege.controlserver)
            .WithArgs(sapi.ChatCommands.Parsers.OnlinePlayer("player"))
            .HandleWith(OnSetKit);
    }

    private TextCommandResult OnSetKit(TextCommandCallingArgs args)
    {
        if (args.Parsers[0].GetValue() is IPlayer foundPlayer)
        {
            var playerData = _playerConfig.GetPlayerDataByUid(foundPlayer.PlayerUID, false);
            if (playerData != null)
            {
                playerData.StarterkitRecived = true;
                playerData.MarkDirty();
                return TextCommandResult.Success(Lang.Get("th3essentials:cd-stp-done", foundPlayer.PlayerName));
            }

            return TextCommandResult.Error(Lang.Get("th3essentials:cd-rstp-npd"));
        }
        return TextCommandResult.Error(Lang.Get("th3essentials:cd-rstp-unknown"));
    }
    
    private TextCommandResult OnResetKit(TextCommandCallingArgs args)
    {
        if (args.Parsers[0].GetValue() is IPlayer foundPlayer)
        {
            var playerData = _playerConfig.GetPlayerDataByUid(foundPlayer.PlayerUID, false);
            if (playerData != null)
            {
                playerData.StarterkitRecived = false;
                playerData.MarkDirty();
                return TextCommandResult.Success(Lang.Get("th3essentials:cd-rstp-done", foundPlayer.PlayerName));
            }

            return TextCommandResult.Error(Lang.Get("th3essentials:cd-rstp-npd"));
        }
        return TextCommandResult.Error(Lang.Get("th3essentials:cd-rstp-unknown"));
    }

    private TextCommandResult OnResetAllKits(TextCommandCallingArgs args)
    {
        if (args.Parsers[0].GetValue() is not string ok || ok != "confirm")
            return TextCommandResult.Success(Lang.Get("th3essentials:cd-rst"));

        var server = (ServerMain)_sapi.World;
        var chunkThread = typeof(ServerMain).GetField("chunkThread", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(server) as ChunkServerThread;
        var gameDatabase = (GameDatabase)typeof(ChunkServerThread).GetField("gameDatabase", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(chunkThread)!;
                
        foreach (var th3d in server.PlayerDataManager.PlayerDataByUid.Values)
        {
            var onwdata = _playerConfig.GetPlayerDataByUid(th3d.PlayerUID, false);
            if (onwdata != null)
            {
                onwdata.StarterkitRecived = false;
                onwdata.MarkDirty();
                _sapi.Logger.Debug("Starterkit for {0} was reset", th3d.LastKnownPlayername);
            }
            else
            {
                var playerData = gameDatabase.GetPlayerData(th3d.PlayerUID);
                if (playerData != null)
                {
                    var swPdata = SerializerUtil.Deserialize<ServerWorldPlayerData>(playerData);
                    var moddata = swPdata.GetModdata(Th3Essentials.Th3EssentialsModDataKey);
                    if (moddata != null)
                    {
                        var th3Pdata = SerializerUtil.Deserialize<Th3PlayerData?>(moddata, null);
                        if (th3Pdata != null)
                        {
                            th3Pdata.StarterkitRecived = false;
                            swPdata.SetModdata(Th3Essentials.Th3EssentialsModDataKey, SerializerUtil.Serialize(th3Pdata));
                            gameDatabase.SetPlayerData(th3d.PlayerUID, SerializerUtil.Serialize(swPdata));
                            continue;
                        }
                    }
                }
                _sapi.Logger.Debug("No Th3PlayerData for player {0} found, no need to reset", th3d.LastKnownPlayername);
            }
        }
        return  TextCommandResult.Success(Lang.Get("th3essentials:cd-rst-alldone"));
    }

    private TextCommandResult OnSetStarterKit(TextCommandCallingArgs args)
    {
        if (_config.Items == null)
        {
            _config.Items = new List<StarterkitItem>();
        }
        else
        {
            _config.Items.Clear();
        }
        var inventory = args.Caller.Player.InventoryManager.GetHotbarInventory();
        foreach (var slot in inventory)
        {
            if (slot.GetType() != typeof(ItemSlotSurvival) || slot.Itemstack == null) continue;
                        
            var enumItemClass = slot.Itemstack.Class;
            var stackSize = slot.Itemstack.StackSize;
            var code = slot.Itemstack.Collectible.Code;

            if (slot.Itemstack.Attributes is not TreeAttribute attributes) continue;
                        
            // remove food perish data
            attributes.RemoveAttribute("transitionstate");

            _config.Items.Add(new StarterkitItem(enumItemClass, code, stackSize, attributes));
        }
        _config.MarkDirty();
        return TextCommandResult.Success(Lang.Get("th3essentials:st-setup"));
    }

    private TextCommandResult TryGiveItemStack(ICoreServerAPI api, IServerPlayer player)
    {
        if (_config.Items == null || _config.Items.Count == 0)
        {
            return TextCommandResult.Success(Lang.Get("th3essentials:st-notsetup"));
        }
        var playerData = _playerConfig.GetPlayerDataByUid(player.PlayerUID);
        if (playerData.StarterkitRecived)
        {
            return TextCommandResult.Success(Lang.Get("th3essentials:st-hasalready"));
        }

        try
        {
            var inventory = player.InventoryManager.GetHotbarInventory();
            var emptySlots = inventory.Count(slot => slot.GetType() == typeof(ItemSlotSurvival) && slot.Empty);
            if (emptySlots < _config.Items.Count)
            {
                return TextCommandResult.Success(Lang.Get("th3essentials:st-needspace", _config.Items.Count));
            }
            for (var i = 0; i < _config.Items.Count; i++)
            {
                var asset = new AssetLocation(_config.Items[i].Code.ToString());
                    
                var received = false;
                switch (_config.Items[i].Itemclass)
                {
                    case EnumItemClass.Item:
                    {
                        var item = api.World.GetItem(asset);

                        if (item != null)
                        {
                            var itemStack = new ItemStack(item, _config.Items[i].Stacksize)
                            {
                                Attributes = TreeAttribute.CreateFromBytes(_config.Items[i].Attributes)
                            };

                            received = player.Entity.TryGiveItemStack(itemStack);
                            if (!received)
                            {
                                _sapi.Logger.Error($"Failed to give starterkit item: {_config.Items[i].Stacksize} x {_config.Items[i].Code}  [{itemStack.Attributes.ToJsonToken()}]");
                            }
                        }
                        break;
                    }
                    case EnumItemClass.Block:
                    {
                        var block = api.World.GetBlock(asset);
                        if (block != null)
                        {
                            var itemStack = new ItemStack(block, _config.Items[i].Stacksize)
                            {
                                Attributes = TreeAttribute.CreateFromBytes(_config.Items[i].Attributes)
                            };

                            received = player.Entity.TryGiveItemStack(itemStack);
                            if (!received)
                            {
                                _sapi.Logger.Error($"Failed to give starterkit item: {_config.Items[i].Stacksize} x {_config.Items[i].Code}  [{itemStack.Attributes.ToJsonToken()}]");
                            }
                        }
                        break;
                    }
                }
                if (!received)
                {
                    playerData.StarterkitRecived = true;
                    playerData.MarkDirty();
                    return TextCommandResult.Error(Lang.Get("th3essentials:st-wrong"));
                }
            }
            playerData.StarterkitRecived = true;
            playerData.MarkDirty();
            return TextCommandResult.Success(Lang.Get("th3essentials:st-recived"));
        }
        catch (Exception e)
        {
            api.Server.LogError(e.Message);
            return TextCommandResult.Error(e.Message);
        }
    }
}