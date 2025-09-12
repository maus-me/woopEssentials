using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace WoopEssentials.Commands;

internal class Ping : Command
{
    private ICoreServerAPI _sapi = null!;

    internal override void Init(ICoreServerAPI api)
    {
        _sapi = api;
        api.ChatCommands.Create("ping")
            .WithDescription(Lang.Get("woopessentials:cd-ping"))
            .RequiresPrivilege(Privilege.chat)
            .HandleWith(OnPing)
            .Validate();
    }

    private TextCommandResult OnPing(TextCommandCallingArgs args)
    {
        return TextCommandResult.Success("Pong");
    }
}