using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace Th3Essentials.Commands;

internal class Serverinfo : Command
{
    internal override void Init(ICoreServerAPI sapi)
    {
        if (Th3Essentials.Config.InfoMessage != null)
        {
            sapi.ChatCommands.Create("serverinfo")
                .WithDescription(Lang.Get("th3essentials:cd-info"))
                .RequiresPlayer()
                .RequiresPrivilege(Privilege.chat)
                .HandleWith(_ => TextCommandResult.Success(Th3Essentials.Config.InfoMessage));
        }
    }
}