using System;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace Th3Essentials.Commands;

internal class Restart : Command
{
    internal override void Init(ICoreServerAPI sapi)
    {
        if (WoopEssentials.Config.ShutdownEnabled)
        {
            sapi.ChatCommands.Create("restart")
                .WithDescription(Lang.Get("woopessentials:cd-restart"))
                .RequiresPrivilege(Privilege.chat)
                .HandleWith(_ =>
                {
                    if (WoopEssentials.Config.ShutdownEnabled)
                    {
                        var restart = WoopEssentials.ShutDownTime - DateTime.Now;
                        var response = Lang.Get("woopessentials:slc-restart-resp", restart.Hours.ToString("D2"),
                            restart.Minutes.ToString("D2"));
                        return TextCommandResult.Success(response);
                    }
                    else
                    {
                        return TextCommandResult.Success(Lang.Get("woopessentials:slc-restart-disabled"));
                    }
                });
        }
    }
}