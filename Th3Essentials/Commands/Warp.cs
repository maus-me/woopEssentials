using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Th3Essentials.Config;
using Th3Essentials.Systems;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace Th3Essentials.Commands
{
    internal class Warp : Command
    {
        private Th3PlayerConfig _playerConfig;

        internal override void Init(ICoreServerAPI sapi)
        {
            if (Th3Essentials.Config.WarpEnabled)
            {
                _playerConfig = Th3Essentials.PlayerConfig;

                sapi.ChatCommands.Create("warp")
                    .WithDescription(Lang.Get("th3essentials:cd-warp"))
                    .RequiresPlayer()
                    .RequiresPrivilege(Privilege.chat)
                    .WithArgs(sapi.ChatCommands.Parsers.Word("action",
                        new[] { "add", "remove", "list", "&ltwarp name&gt" }),
                        sapi.ChatCommands.Parsers.OptionalWord("warp_name"))
                    .HandleWith(OnWarp);
            }
        }

        private TextCommandResult OnWarp(TextCommandCallingArgs args)
        {
            var cmd = args.Parsers[0].GetValue() as string;

            var player = args.Caller.Player;
            switch (cmd)
            {
                case "add":
                {
                    if (!player.HasPrivilege(Privilege.controlserver))
                    {
                        break;
                    }

                    var warpName = args.Parsers[1].GetValue() as string;

                    if (warpName == string.Empty)
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
                        break;
                    }

                    var warpName = args.Parsers[1].GetValue() as string;

                    if (Th3Essentials.Config.WarpLocations == null)
                        return TextCommandResult.Success(Lang.Get("th3essentials:wp-removed", warpName));
                    
                    var warpPoint = Th3Essentials.Config.FindWarpByName(warpName);
                    Th3Essentials.Config.WarpLocations.Remove(warpPoint);
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
                    var warpName = args.Parsers[0].GetValue() as string;

                    if (warpName == string.Empty)
                        return TextCommandResult.Error(Lang.Get("th3essentials:wp-notfound", ""));
                    
                    var playerData = _playerConfig.GetPlayerDataByUID(player.PlayerUID);
                    if (player.WorldData.CurrentGameMode == EnumGameMode.Creative ||
                        Homesystem.CanTravel(playerData))
                    {
                        var warpPoint = Th3Essentials.Config.FindWarpByName(warpName);
                        if (warpPoint == null)
                            return TextCommandResult.Success(Lang.Get("th3essentials:wp-notfound", warpName));
                        
                        Homesystem.TeleportTo(player, playerData, warpPoint.Position);
                        return TextCommandResult.Success(Lang.Get("th3essentials:wp-to", warpName));

                    }

                    var diff = playerData.HomeLastuseage.AddSeconds(Th3Essentials.Config.HomeCooldown) -
                               DateTime.Now;
                    return TextCommandResult.Success(Lang.Get("th3essentials:hs-wait", diff.Minutes,
                        diff.Seconds));

                }
            }

            throw new UnreachableException("Unknown warp command");
        }
    }
}