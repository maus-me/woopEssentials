using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace Th3Essentials.Commands;

internal class Smite : Command
{
    private ICoreServerAPI _sapi;
    internal override void Init(ICoreServerAPI api)
    {
        if (!Th3Essentials.Config.EnableSmite) return;
        
        _sapi = api;
        api.ChatCommands.Create("smite")
            .WithDescription(Lang.Get("th3essentials:cd-smite-desc"))
            .RequiresPrivilege(Privilege.commandplayer)
            .RequiresPlayer()
            .WithArgs(api.ChatCommands.Parsers.OptionalWord("player"))
            .HandleWith(OnSmite);
    }

    private TextCommandResult OnSmite(TextCommandCallingArgs args)
    {
        var weatherSystemServer = _sapi.ModLoader.GetModSystem<WeatherSystemServer>();
        var playerName = args.Parsers[0].GetValue() as string;

        if (!string.IsNullOrEmpty(playerName))
        {
            var player = _sapi.World.AllOnlinePlayers.FirstOrDefault(p => p.PlayerName.Equals(playerName));

            if (player != null)
            {
                weatherSystemServer.SpawnLightningFlash(new Vec3d(player.Entity.Pos));
                return TextCommandResult.Success(Lang.Get("th3essentials:cd-smite-spl", player.PlayerName));
            } 
            return TextCommandResult.Error(Lang.Get("th3essentials:cd-smite-clfp",playerName));
        }

        if (args.Caller.Player.CurrentBlockSelection != null)
        {
            weatherSystemServer.SpawnLightningFlash(args.Caller.Player.CurrentBlockSelection.Position.ToVec3d());
            return TextCommandResult.Success();
        }
        
        if (args.Caller.Player.CurrentEntitySelection != null)
        {
            weatherSystemServer.SpawnLightningFlash(args.Caller.Player.CurrentEntitySelection.Position);
            return TextCommandResult.Success(Lang.Get("th3essentials:cd-smite-sponen", args.Caller.Player.CurrentEntitySelection.Entity.GetName()));
        }

        return TextCommandResult.Error(Lang.Get("th3essentials:cd-smite-unable"));
    }
}