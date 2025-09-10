using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Th3Essentials.Config;
using Th3Essentials.Systems;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Th3Essentials.Commands;

internal class Warp : Command
{
    private Th3PlayerConfig _playerConfig = null!;
    private Th3Config _config = null!;

    internal override void Init(ICoreServerAPI sapi)
    {
        if (Th3Essentials.Config.WarpEnabled)
        {
            _playerConfig = Th3Essentials.PlayerConfig;
            _config = Th3Essentials.Config;

            sapi.ChatCommands.Create("warp")
                .WithDescription(Lang.Get("th3essentials:cd-warp"))
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
            return TextCommandResult.Success(Lang.Get("th3essentials:wp-item-unset"));
        }
        
        var enumItemClass = slot.Itemstack.Class;
        var stackSize = slot.Itemstack.StackSize;
        var code = slot.Itemstack.Collectible.Code;

        if (slot.Itemstack.Attributes is not TreeAttribute attributes) return TextCommandResult.Success("error not a TreeAttribute");
                        
        // remove food perish data
        attributes.RemoveAttribute("transitionstate");

        _config.WarpItem = new StarterkitItem(enumItemClass, code, stackSize, attributes);
        _config.MarkDirty();
        return TextCommandResult.Success(Lang.Get("th3essentials:wp-item-set"));
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
                    return TextCommandResult.Success(Lang.Get("th3essentials:cd-all-notallow"));
                }
                return SetItem(args);
            }
            case "add":
            {
                if (!player.HasPrivilege(Privilege.controlserver))
                {
                    return TextCommandResult.Success(Lang.Get("th3essentials:cd-all-notallow"));
                }

                var warpName = (string)args.Parsers[1].GetValue();

                if (string.IsNullOrWhiteSpace(warpName))
                {
                    return TextCommandResult.Error(Lang.Get("th3essentials:wp-no-name"));
                }

                if (Th3Essentials.Config.FindWarpByName(warpName) != null)
                {
                    return TextCommandResult.Error(Lang.Get("th3essentials:wp-exists", warpName));
                }

                Th3Essentials.Config.WarpLocations ??= new List<HomePoint>();

                Th3Essentials.Config.WarpLocations.Add(new HomePoint(warpName, player.Entity.Pos.AsBlockPos));
                Th3Essentials.Config.MarkDirty();
                return TextCommandResult.Success(Lang.Get("th3essentials:wp-added", warpName));
            }
            case "remove":
            {
                if (!player.HasPrivilege(Privilege.controlserver))
                {
                    return TextCommandResult.Success(Lang.Get("th3essentials:cd-all-notallow"));
                }

                var warpName = (string)args.Parsers[1].GetValue();

                if (Th3Essentials.Config.WarpLocations == null)
                    return TextCommandResult.Success(Lang.Get("th3essentials:wp-removed", warpName));
                    
                var warpPoint = Th3Essentials.Config.FindWarpByName(warpName);
                if (warpPoint != null) Th3Essentials.Config.WarpLocations.Remove(warpPoint);
                Th3Essentials.Config.MarkDirty();

                return TextCommandResult.Success(Lang.Get("th3essentials:wp-removed", warpName));
            }
            case "list":
            {
                var response = Lang.Get("th3essentials:wp-list") + "\n";

                if (Th3Essentials.Config.WarpLocations != null)
                {
                    response = Th3Essentials.Config.WarpLocations.Aggregate(response, (current, warpPoint) => current + (warpPoint.Name + "\n"));
                }

                return TextCommandResult.Success(response);
            }
            default:
            {
                var warpName = (string)args.Parsers[0].GetValue();

                if (warpName == string.Empty)
                    return TextCommandResult.Error(Lang.Get("th3essentials:wp-notfound", ""));
                    
                var playerData = _playerConfig.GetPlayerDataByUid(player.PlayerUID);
                var playerConfig = Homesystem.GetConfig(player, playerData, _config); 
                if (!playerConfig.WarpEnabled)
                {
                    return TextCommandResult.Success(Lang.Get("th3essentials:cd-all-notallow"));
                }
                if (player.WorldData.CurrentGameMode == EnumGameMode.Creative ||
                    CanTravel(playerData))
                {
                    var warpPoint = Th3Essentials.Config.FindWarpByName(warpName);
                    if (warpPoint == null)
                        return TextCommandResult.Success(Lang.Get("th3essentials:wp-notfound", warpName));
                        
                    if (Homesystem.CheckPayment(_config.WarpItem, playerConfig.WarpCost, player, out var canTeleport, out var success)) return success!;

                    if (canTeleport)
                    {
                        Homesystem.PayIfNeeded(player, _config.WarpItem, playerConfig.WarpCost);
                        TeleportTo(player, playerData, warpPoint.Position);
                        return TextCommandResult.Success(Lang.Get("th3essentials:wp-to", warpName));
                    }

                    return TextCommandResult.Success("Could not teleport");
                }

                TimeSpan diff;
                if (_config.WarpCooldown >= 0)
                {
                    diff = playerData.WarpLastUsage.AddSeconds(Th3Essentials.Config.WarpCooldown) -
                           DateTime.Now;
                }
                else
                {
                    diff = playerData.WarpLastUsage.AddSeconds(Th3Essentials.Config.WarpCooldown) -
                           DateTime.Now;
                }
                return TextCommandResult.Success(Lang.Get("th3essentials:wait-time", Th3Util.PrettyTime(diff)));

            }
        }
    }
    
    public bool CanTravel(Th3PlayerData playerData)
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
    
    public void TeleportTo(IPlayer player, Th3PlayerData playerData, BlockPos location)
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