using System.Collections.Generic;
using Th3Essentials.Config;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace Th3Essentials.Commands;

internal class WoopConfigCommands : Command
{

    private WoopConfig _config = null!;
    private ICoreServerAPI _sapi = null!;

    internal override void Init(ICoreServerAPI api)
    {
        _sapi = api;
        _config = WoopEssentials.Config;
        api.ChatCommands.Create("th3config")
            .WithDescription(Lang.Get("woopessentials:cd-rtp"))
            .RequiresPrivilege(Privilege.controlserver)
            
            .BeginSubCommand("addRole")
                .RequiresPrivilege(Privilege.controlserver)
                .WithDescription("adds a role")
                .WithArgs(_sapi.ChatCommands.Parsers.Word("role_code"), 
                    _sapi.ChatCommands.Parsers.Int("homelimit"),
                    _sapi.ChatCommands.Parsers.Int("home_cost"), 
                    _sapi.ChatCommands.Parsers.Int("back_cost"), 
                    _sapi.ChatCommands.Parsers.Int("sethome_cost"), 
                    _sapi.ChatCommands.Parsers.Int("rtp_cost"), 
                    _sapi.ChatCommands.Parsers.Int("t2p_cost"), 
                    _sapi.ChatCommands.Parsers.Bool("rtp_enabled"), 
                    _sapi.ChatCommands.Parsers.Bool("t2p_enabled"),
                    _sapi.ChatCommands.Parsers.Bool("warp_enabled"),
                    _sapi.ChatCommands.Parsers.Int("warp_cost") )
                .HandleWith(AddRole)
            .EndSubCommand()
            
            .BeginSubCommand("removeRole")
                .RequiresPrivilege(Privilege.controlserver)
                .WithDescription("removes a role")
                .WithArgs(_sapi.ChatCommands.Parsers.Word("role_code"))
                .HandleWith(RemoveRole)
            .EndSubCommand();
    }

    private TextCommandResult AddRole(TextCommandCallingArgs args)
    {
        var code = args.Parsers[0].GetValue() as string;
        var homeLimit = (int)args.Parsers[1].GetValue();
        var homeCost = (int)args.Parsers[2].GetValue();
        var backCost = (int)args.Parsers[3].GetValue();
        var setHomeCost = (int)args.Parsers[4].GetValue();
        var rtpCost = (int)args.Parsers[5].GetValue();
        var teleportToPlayerCost = (int)args.Parsers[6].GetValue();
        var rtpEnabled = (bool)args.Parsers[7].GetValue();
        var t2PEnabled = (bool)args.Parsers[8].GetValue();
        var warpEnabled = (bool)args.Parsers[9].GetValue();
        var warpCost = (int)args.Parsers[10].GetValue();

        _config.RoleConfig ??= new Dictionary<string, RoleConfig>();

        _config.RoleConfig[code!] = new RoleConfig(homeLimit,homeCost, backCost, setHomeCost, rtpCost, teleportToPlayerCost , rtpEnabled, t2PEnabled, warpEnabled, warpCost);
        _config.MarkDirty();
        return TextCommandResult.Success("added config for role");
    }

    private TextCommandResult RemoveRole(TextCommandCallingArgs args)
    {
        var code = args.Parsers[0].GetValue() as string;
        if (_config.RoleConfig != null)
        {
            _config.RoleConfig.Remove(code!);
            _config.MarkDirty();
        }
        return TextCommandResult.Success("removed config for role");
    }
}