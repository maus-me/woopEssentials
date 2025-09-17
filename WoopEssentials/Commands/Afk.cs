using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using WoopEssentials.Systems;

namespace WoopEssentials.Commands;

internal class Afk : Command
{
    internal override void Init(ICoreServerAPI api)
    {
        api.ChatCommands.Create("afk")
            .WithDescription(Lang.Get("woopessentials:cd-afk"))
            .RequiresPrivilege(Privilege.chat)
            .RequiresPlayer()
            .HandleWith(OnAfk)
            .Validate();
    }

    private TextCommandResult OnAfk(TextCommandCallingArgs args)
    {
        if (args.Caller.Player is not IServerPlayer sp)
        {
            return TextCommandResult.Success("AFK only available for players");
        }

        AfkSystem.Instance.ToggleAfk(sp);
        return TextCommandResult.Success();
    }
}
