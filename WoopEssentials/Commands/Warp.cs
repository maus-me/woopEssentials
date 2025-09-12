using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using WoopEssentials.Config;
using WoopEssentials.Systems;

namespace WoopEssentials.Commands;

internal class Warp : Command
{
    private WoopPlayerConfig _playerConfig = null!;
    private WoopConfig _config = null!;

    internal override void Init(ICoreServerAPI sapi)
    {
        if (WoopEssentials.Config.WarpEnabled)
        {
            _playerConfig = WoopEssentials.PlayerConfig;
            _config = WoopEssentials.Config;

            sapi.ChatCommands.Create("warp")
                .WithDescription(Lang.Get("woopessentials:cd-warp"))
                .RequiresPlayer()
                .RequiresPrivilege(Privilege.chat)
                .WithArgs(sapi.ChatCommands.Parsers.Word("action",
                        new[] { "add", "remove", "list","setitem", "&ltwarp name&gt" }),
                    sapi.ChatCommands.Parsers.OptionalWord("warp_name"))
                .HandleWith(OnWarp)
                ;
        }
    }
    
    private TextCommandResult SetItem(TextCommandCallingArgs args)
    {
        var slot = args.Caller.Player.InventoryManager.ActiveHotbarSlot;

        if (slot.Itemstack == null)
        {
            _config.WarpItem = null;
            return TextCommandResult.Success(Lang.Get("woopessentials:wp-item-unset"));
        }
        
        var enumItemClass = slot.Itemstack.Class;
        var stackSize = slot.Itemstack.StackSize;
        var code = slot.Itemstack.Collectible.Code;

        if (slot.Itemstack.Attributes is not TreeAttribute attributes) return TextCommandResult.Success("error not a TreeAttribute");
                        
        // remove food perish data
        attributes.RemoveAttribute("transitionstate");

        _config.WarpItem = new StarterkitItem(enumItemClass, code, stackSize, attributes);
        _config.MarkDirty();
        return TextCommandResult.Success(Lang.Get("woopessentials:wp-item-set"));
    }

    private TextCommandResult OnWarp(TextCommandCallingArgs args)
    {
        var cmd = args.Parsers[0].GetValue() as string;

        var player = args.Caller.Player;
        switch (cmd)
        {
            case "setitem":
            {
                if (!player.HasPrivilege(Privilege.controlserver))
                {
                    return TextCommandResult.Success(Lang.Get("woopessentials:cd-all-notallow"));
                }
                return SetItem(args);
            }
            case "add":
            {
                if (!player.HasPrivilege(Privilege.controlserver))
                {
                    return TextCommandResult.Success(Lang.Get("woopessentials:cd-all-notallow"));
                }

                var warpName = (string)args.Parsers[1].GetValue();

                if (string.IsNullOrWhiteSpace(warpName))
                {
                    return TextCommandResult.Error(Lang.Get("woopessentials:wp-no-name"));
                }

                if (WoopEssentials.Config.FindWarpByName(warpName) != null)
                {
                    return TextCommandResult.Error(Lang.Get("woopessentials:wp-exists", warpName));
                }

                WoopEssentials.Config.WarpLocations ??= new List<HomePoint>();

                WoopEssentials.Config.WarpLocations.Add(new HomePoint(warpName, player.Entity.Pos.AsBlockPos));
                WoopEssentials.Config.MarkDirty();
                return TextCommandResult.Success(Lang.Get("woopessentials:wp-added", warpName));
            }
            case "remove":
            {
                if (!player.HasPrivilege(Privilege.controlserver))
                {
                    return TextCommandResult.Success(Lang.Get("woopessentials:cd-all-notallow"));
                }

                var warpName = (string)args.Parsers[1].GetValue();

                if (WoopEssentials.Config.WarpLocations == null)
                    return TextCommandResult.Success(Lang.Get("woopessentials:wp-removed", warpName));
                    
                var warpPoint = WoopEssentials.Config.FindWarpByName(warpName);
                if (warpPoint != null) WoopEssentials.Config.WarpLocations.Remove(warpPoint);
                WoopEssentials.Config.MarkDirty();

                return TextCommandResult.Success(Lang.Get("woopessentials:wp-removed", warpName));
            }
            case "list":
            {
                var response = Lang.Get("woopessentials:wp-list") + "\n";

                if (WoopEssentials.Config.WarpLocations != null)
                {
                    response = WoopEssentials.Config.WarpLocations.Aggregate(response, (current, warpPoint) => current + (warpPoint.Name + "\n"));
                }

                return TextCommandResult.Success(response);
            }
            default:
            {
                var warpName = (string)args.Parsers[0].GetValue();

                if (warpName == string.Empty)
                    return TextCommandResult.Error(Lang.Get("woopessentials:wp-notfound", ""));

                // Check if player is in PvP mode or has PvP cooldown
                if (!EntityBehaviorPvp.CheckPvP(player, out var errorMessage))
                {
                    return TextCommandResult.Success(errorMessage);
                }

                var playerData = _playerConfig.GetPlayerDataByUid(player.PlayerUID);
                var playerConfig = Homesystem.GetConfig(player, playerData, _config); 
                if (!playerConfig.WarpEnabled)
                {
                    return TextCommandResult.Success(Lang.Get("woopessentials:cd-all-notallow"));
                }
                if (player.WorldData.CurrentGameMode == EnumGameMode.Creative ||
                    CanTravel(playerData))
                {
                    var warpPoint = WoopEssentials.Config.FindWarpByName(warpName);
                    if (warpPoint == null)
                        return TextCommandResult.Success(Lang.Get("woopessentials:wp-notfound", warpName));
                        
                    if (Homesystem.CheckPayment(_config.WarpItem, playerConfig.WarpCost, player, out var canTeleport, out var success)) return success!;

                    if (canTeleport)
                    {
                        Homesystem.PayIfNeeded(player, _config.WarpItem, playerConfig.WarpCost);
                        TeleportTo(player, playerData, warpPoint.Position);
                        return TextCommandResult.Success(Lang.Get("woopessentials:wp-to", warpName));
                    }

                    return TextCommandResult.Success("Could not teleport");
                }

                TimeSpan diff;
                if (_config.WarpCooldown >= 0)
                {
                    diff = playerData.WarpLastUsage.AddSeconds(WoopEssentials.Config.WarpCooldown) -
                           DateTime.Now;
                }
                else
                {
                    diff = playerData.WarpLastUsage.AddSeconds(WoopEssentials.Config.WarpCooldown) -
                           DateTime.Now;
                }
                return TextCommandResult.Success(Lang.Get("woopessentials:wait-time", WoopUtil.PrettyTime(diff)));

            }
        }
    }
    
    public bool CanTravel(WoopPlayerData playerData)
    {
        if (_config.WarpCooldown >= 0)
        {
            var canTravel = playerData.WarpLastUsage.AddSeconds(_config.WarpCooldown);
            return canTravel <= DateTime.Now;
        }
        else
        {
            var canTravel = playerData.HomeLastuseage.AddSeconds(_config.HomeCooldown);
            return canTravel <= DateTime.Now;
        }
    }
    
    public void TeleportTo(IPlayer player, WoopPlayerData playerData, BlockPos location)
    {
        if (_config.WarpCooldown >= 0)
        {
            player.Entity.TeleportTo(new Vec3d(location.X + 0.5,location.Y + 0.2,location.Z + 0.5));
            playerData.WarpLastUsage = DateTime.Now;
            playerData.MarkDirty();
        }
        else
        {
            player.Entity.TeleportTo(new Vec3d(location.X + 0.5,location.Y + 0.2,location.Z + 0.5));
            playerData.HomeLastuseage = DateTime.Now;
            playerData.MarkDirty();
            
        }
        
    }
}