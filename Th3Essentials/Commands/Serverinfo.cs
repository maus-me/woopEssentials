using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace Th3Essentials.Commands;

internal class Serverinfo : Command
{
    internal override void Init(ICoreServerAPI sapi)
    {
        if (WoopEssentials.Config.InfoMessage != null)
        {
            sapi.ChatCommands.Create("serverinfo")
                .WithDescription(Lang.Get("woopessentials:cd-info"))
                .RequiresPlayer()
                .RequiresPrivilege(Privilege.chat)
                .HandleWith(_ => TextCommandResult.Success(WoopEssentials.Config.InfoMessage));
        }
    }
}