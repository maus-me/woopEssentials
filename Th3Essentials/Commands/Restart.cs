using System;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace Th3Essentials.Commands
{
    internal class Restart : Command
    {
        internal override void Init(ICoreServerAPI sapi)
        {
            if (Th3Essentials.Config.ShutdownEnabled)
            {
                sapi.ChatCommands.Create("restart")
                    .WithDescription(Lang.Get("th3essentials:cd-restart"))
                    .RequiresPrivilege(Privilege.chat)
                    .HandleWith(_ =>
                    {
                        if (Th3Essentials.Config.ShutdownEnabled)
                        {
                            var restart = Th3Essentials.ShutDownTime - DateTime.Now;
                            var response = Lang.Get("th3essentials:slc-restart-resp", restart.Hours.ToString("D2"),
                                restart.Minutes.ToString("D2"));
                            return TextCommandResult.Success(response);
                        }
                        else
                        {
                            return TextCommandResult.Success(Lang.Get("th3essentials:slc-restart-disabled"));
                        }
                    });
            }
        }
    }
}