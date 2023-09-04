using System;
using System.Collections.Generic;
using System.Linq;
using Th3Essentials.Config;
using Th3Essentials.Systems;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;

namespace Th3Essentials.Commands;

internal class TeleportRequest : Command
{
    private Th3PlayerConfig _playerConfig;

    private Th3Config _config;

    private ICoreServerAPI _sapi;

    private Dictionary<string, string> _tpRequests;

    internal override void Init(ICoreServerAPI api)
    {
        _sapi = api;
        _playerConfig = Th3Essentials.PlayerConfig;
        _config = Th3Essentials.Config;
        if (_config.TeleportToPlayerEnabled)
        {
            _sapi = api;
            _tpRequests = new Dictionary<string, string>();
            api.ChatCommands.Create("t2p")
                .WithDescription(Lang.Get("th3essentials:cd-t2pr"))
                .RequiresPlayer()
                .RequiresPrivilege(Privilege.chat)
                
                .BeginSubCommand("request")
                    .WithAlias("r")
                    .RequiresPrivilege(Privilege.chat)
                    .RequiresPlayer()
                    .WithDescription("Request a teleport to a player")
                    .WithArgs(_sapi.ChatCommands.Parsers.OnlinePlayer("player"))
                    .HandleWith(OnT2Pr)
                .EndSubCommand()
                
                .BeginSubCommand("abort")
                    .RequiresPrivilege(Privilege.chat)
                    .RequiresPlayer()
                    .WithDescription("Abort request to teleport to a player")
                    .WithArgs(_sapi.ChatCommands.Parsers.OnlinePlayer("player"))
                    .HandleWith(OnAbortT2p)
                .EndSubCommand()
                
                .BeginSubCommand("accept")
                    .WithAlias("a")
                    .RequiresPrivilege(Privilege.chat)
                    .RequiresPlayer()
                    .WithDescription("Accept the TP request [yes/no] default is yes if noting is specified")
                    .WithArgs(_sapi.ChatCommands.Parsers.OptionalBool("accept"))
                    .HandleWith(AcceptTp)
                .EndSubCommand()
                
                .BeginSubCommand("item")
                    .RequiresPrivilege(Privilege.controlserver)
                    .RequiresPlayer()
                    .WithDescription("Sets the current hotbar slot as the required item and quantity, use empty slot to unset")
                    .HandleWith(SetItem)
                .EndSubCommand()
                ;
        }
    }

    private TextCommandResult OnAbortT2p(TextCommandCallingArgs args)
    {
        var otherPlayer = args.Parsers[0].GetValue() as IPlayer;
        if (_tpRequests.ContainsKey(otherPlayer.PlayerUID))
        {
            _tpRequests.Remove(otherPlayer.PlayerUID);
            return TextCommandResult.Success($"Teleport request to {otherPlayer.PlayerName} aborted");
        }
        return TextCommandResult.Success($"No request for that player found");
    }

    private TextCommandResult AcceptTp(TextCommandCallingArgs args)
    {
        var accept = args.Parsers[0].IsMissing || (bool)args.Parsers[0].GetValue();

        string? requesterUID;
        if (_tpRequests.TryGetValue(args.Caller.Player.PlayerUID, out requesterUID))
        {
            _tpRequests.Remove(args.Caller.Player.PlayerUID);
        }
        
        if (accept)
        {
            if (requesterUID != null)
            {
                var requestingPlayer = _sapi.World.AllOnlinePlayers.FirstOrDefault(p => p.PlayerUID.Equals(requesterUID));
                if (requestingPlayer == null)
                {
                    return TextCommandResult.Success("Player seems not online anymore");
                }
                var playerData = _playerConfig.GetPlayerDataByUID(requestingPlayer.PlayerUID);

                TeleportTo(requestingPlayer, playerData ,args.Caller.Player.Entity.Pos);
            }
            else
            {
                return TextCommandResult.Success("Maybe other player aborted the request");
            }
        }
        return TextCommandResult.Success("You declined the teleport request");
    }

    private TextCommandResult SetItem(TextCommandCallingArgs args)
    {
        var slot = args.Caller.Player.InventoryManager.ActiveHotbarSlot;

        if (slot.Itemstack == null)
        {
            _config.TeleportToPlayerItem = null;
            return TextCommandResult.Success("T2PR Item unset");
        }
        
        var enumItemClass = slot.Itemstack.Class;
        var stackSize = slot.Itemstack.StackSize;
        var code = slot.Itemstack.Collectible.Code;

        if (slot.Itemstack.Attributes is not TreeAttribute attributes) return TextCommandResult.Success("error not a TreeAttribute");
                        
        // remove food perish data
        attributes.RemoveAttribute("transitionstate");

        _config.TeleportToPlayerItem = new StarterkitItem(enumItemClass, code, stackSize, attributes);
        _config.MarkDirty();
        return TextCommandResult.Success("T2PR Item set");
    }


    private TextCommandResult OnT2Pr(TextCommandCallingArgs args)
    {
        var otherPlayer = args.Parsers[0].GetValue() as IPlayer;
        
        if (_tpRequests.ContainsKey(otherPlayer.PlayerUID))
        {
            return TextCommandResult.Success("Player already has a pending t2p request");
        }
        
        var player = args.Caller.Player;
        var playerData = _playerConfig.GetPlayerDataByUID(player.PlayerUID);

        var playerConfig = Homesystem.GetConfig(player, playerData, _config);
        
        

        if (!playerConfig.TeleportToPlayerEnabled)
        {
            return TextCommandResult.Success("You are not allowed to use this command");
        }
        
        if (player.WorldData.CurrentGameMode == EnumGameMode.Creative || CanTravel(playerData))
        {
            if (Homesystem.CheckPayment(_config.TeleportToPlayerItem, playerConfig.TeleportToPlayerCost, player, out var canTeleport, out var success)) return success!;
            
            if (canTeleport && player.InventoryManager.ActiveHotbarSlot != null)
            {
                Homesystem.PayIfNeeded(player, _config.TeleportToPlayerItem, playerConfig.TeleportToPlayerCost);
                _tpRequests.Add(otherPlayer.PlayerUID, player.PlayerUID);
                (otherPlayer as IServerPlayer)?.SendMessage(GlobalConstants.GeneralChatGroup, $"Player {player.PlayerName} reqested a teleport to you. \"/t2p a\" or \"/t2p a no\"",EnumChatType.Notification);
                return TextCommandResult.Success(Lang.Get("th3essentials:t2p-success"));
            }

            return TextCommandResult.Error("Something went wrong");
        }
        
        var diff = playerData.T2PLastUsage.AddSeconds(_config.TeleportToPlayerCooldown) - DateTime.Now;
        return TextCommandResult.Success(Lang.Get("th3essentials:hs-wait", diff.Minutes, diff.Seconds));
    }

    public static void TeleportTo(IPlayer player, Th3PlayerData playerData, EntityPos location)
    {
        player.Entity.TeleportTo(location);
        playerData.T2PLastUsage = DateTime.Now;
        playerData.MarkDirty();
    }

    public static bool CanTravel(Th3PlayerData playerData)
    {
        var canTravel = playerData.T2PLastUsage.AddSeconds(Th3Essentials.Config.TeleportToPlayerCooldown);
        return canTravel <= DateTime.Now;
    }
}