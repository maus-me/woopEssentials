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
    private Th3PlayerConfig _playerConfig = null!;

    private Th3Config _config = null!;

    private ICoreServerAPI _sapi = null!;

    private Dictionary<string, string> _tpRequests = null!;

    internal override void Init(ICoreServerAPI api)
    {
        _sapi = api;
        _config = Th3Essentials.Config;
        if (_config.TeleportToPlayerEnabled)
        {
            _playerConfig = Th3Essentials.PlayerConfig;
            _sapi = api;
            _tpRequests = new Dictionary<string, string>();
            api.ChatCommands.Create("t2p")
                .WithDescription(Lang.Get("th3essentials:cd-t2pr"))
                .RequiresPlayer()
                .RequiresPrivilege(Privilege.chat)
                
                .BeginSubCommand("request")
                    .WithAlias("r")
                    .WithRootAlias("t2pr")
                    .RequiresPrivilege(Privilege.chat)
                    .RequiresPlayer()
                    .WithDescription(Lang.Get("th3essentials:cd-t2pr-e"))
                    .WithArgs(_sapi.ChatCommands.Parsers.OnlinePlayer("player"))
                    .HandleWith(OnT2Pr)
                .EndSubCommand()
                
                .BeginSubCommand("abort")
                    .RequiresPrivilege(Privilege.chat)
                    .RequiresPlayer()
                    .WithDescription(Lang.Get("th3essentials:cd-t2pr-a"))
                    .WithArgs(_sapi.ChatCommands.Parsers.OnlinePlayer("player"))
                    .HandleWith(OnAbortT2p)
                .EndSubCommand()
                
                .BeginSubCommand("accept")
                    .WithAlias("a")
                    .WithRootAlias("t2pa")
                    .RequiresPrivilege(Privilege.chat)
                    .RequiresPlayer()
                    .WithDescription(Lang.Get("th3essentials:cd-t2pr-ac"))
                    .WithArgs(_sapi.ChatCommands.Parsers.OptionalBool("accept"))
                    .HandleWith(AcceptTp)
                .EndSubCommand()
                
                .BeginSubCommand("item")
                    .RequiresPrivilege(Privilege.controlserver)
                    .RequiresPlayer()
                    .WithDescription(Lang.Get("th3essentials:cd-t2pr-sc"))
                    .HandleWith(SetItem)
                .EndSubCommand()
                ;
        }
    }

    private TextCommandResult OnAbortT2p(TextCommandCallingArgs args)
    {
        var otherPlayer = (IPlayer)args.Parsers[0].GetValue();
        if (_tpRequests.ContainsKey(otherPlayer.PlayerUID))
        {
            _tpRequests.Remove(otherPlayer.PlayerUID);
            return TextCommandResult.Success(Lang.Get("th3essentials:cd-t2pr-ra",otherPlayer.PlayerName));
        }
        return TextCommandResult.Success(Lang.Get("th3essentials:cd-t2pr-nr"));
    }

    private TextCommandResult AcceptTp(TextCommandCallingArgs args)
    {
        var accept = args.Parsers[0].IsMissing || (bool)args.Parsers[0].GetValue();

        _tpRequests.Remove(args.Caller.Player.PlayerUID, out var requesterUid);
        
        if (accept)
        {
            if (requesterUid != null)
            {
                var requestingPlayer = _sapi.World.AllOnlinePlayers.FirstOrDefault(p => p.PlayerUID.Equals(requesterUid));
                if (requestingPlayer == null)
                {
                    //player not online anymore
                    return TextCommandResult.Success(Lang.Get("th3essentials:cd-t2pr-no"));
                }

                var requestingPlayerData = _playerConfig.GetPlayerDataByUid(requestingPlayer.PlayerUID);
                var requestingplayerConfig = Homesystem.GetConfig(requestingPlayer, requestingPlayerData, _config);

                if (Homesystem.CheckPayment(_config.TeleportToPlayerItem, requestingplayerConfig.TeleportToPlayerCost, requestingPlayer, out var canTeleport, out var success)) return success!;

                if (canTeleport)
                {
                    Homesystem.PayIfNeeded(requestingPlayer, _config.TeleportToPlayerItem, requestingplayerConfig.TeleportToPlayerCost);
                    TeleportTo(requestingPlayer, requestingPlayerData, args.Caller.Player.Entity.Pos);
                }
            }
            else
            {
                return TextCommandResult.Success(Lang.Get("th3essentials:cd-t2pr-my"));
            }
        }
        return TextCommandResult.Success(Lang.Get("th3essentials:cd-t2pr-de"));
    }

    private TextCommandResult SetItem(TextCommandCallingArgs args)
    {
        var slot = args.Caller.Player.InventoryManager.ActiveHotbarSlot;

        if (slot.Itemstack == null)
        {
            _config.TeleportToPlayerItem = null;
            return TextCommandResult.Success(Lang.Get("th3essentials:hs-item-unset"));
        }
        
        var enumItemClass = slot.Itemstack.Class;
        var stackSize = slot.Itemstack.StackSize;
        var code = slot.Itemstack.Collectible.Code;

        if (slot.Itemstack.Attributes is not TreeAttribute attributes) return TextCommandResult.Success("error not a TreeAttribute");
                        
        // remove food perish data
        attributes.RemoveAttribute("transitionstate");

        _config.TeleportToPlayerItem = new StarterkitItem(enumItemClass, code, stackSize, attributes);
        _config.MarkDirty();
        return TextCommandResult.Success(Lang.Get("th3essentials:hs-item-set"));
    }


    private TextCommandResult OnT2Pr(TextCommandCallingArgs args)
    {
        var otherPlayer = (IPlayer)args.Parsers[0].GetValue();
        
        if (_tpRequests.ContainsKey(otherPlayer.PlayerUID))
        {
            return TextCommandResult.Success(Lang.Get("th3essentials:cd-t2pr-pr"));
        }
        
        var player = args.Caller.Player;
        var playerData = _playerConfig.GetPlayerDataByUid(player.PlayerUID);

        var playerConfig = Homesystem.GetConfig(player, playerData, _config);
        
        

        if (!playerConfig.TeleportToPlayerEnabled)
        {
            return TextCommandResult.Success(Lang.Get("th3essentials:cd-all-notallow"));
        }
        
        if (player.WorldData.CurrentGameMode == EnumGameMode.Creative || CanTravel(playerData))
        {
            if (Homesystem.CheckPayment(_config.TeleportToPlayerItem, playerConfig.TeleportToPlayerCost, player, out var canTeleport, out var success)) return success!;
            
            if (canTeleport)
            {
                _tpRequests.Add(otherPlayer.PlayerUID, player.PlayerUID);
                (otherPlayer as IServerPlayer)?.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("th3essentials:cd-t2pr-prm", player.PlayerName),EnumChatType.Notification);
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