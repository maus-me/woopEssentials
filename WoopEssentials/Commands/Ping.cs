using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace WoopEssentials.Commands;

internal class Ping : Command
{
    internal override void Init(ICoreServerAPI api)
    {
        api.ChatCommands.Create("ping")
            .WithDescription(Lang.Get("woopessentials:cd-ping"))
            .RequiresPrivilege(Privilege.chat)
            .RequiresPlayer()
            .HandleWith(OnPing)
            .Validate();
    }



    private TextCommandResult OnPing(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null)
        {
            return TextCommandResult.Success("Pong");
        }

        // Get player's ping in milliseconds (convert from seconds)
        int pingMs = (int)(player.Ping * 1000);

        return TextCommandResult.Success(Lang.Get("woopessentials:ping-ms", pingMs));
    }

}