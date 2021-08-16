using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace Th3Essentials.Commands
{
    internal class Restart : Command
    {
        internal override void Init(ICoreServerAPI api)
        {
            api.RegisterCommand("restart", Lang.Get("th3essentials:cd-msg"), Lang.Get("th3essentials:cd-msg-param"),
                (IServerPlayer player, int groupId, CmdArgs args) =>
                {

                }, Privilege.chat);
        }
    }
}