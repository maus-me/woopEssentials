using System.Collections.Generic;
using CBSEssentials.Config;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace CBSEssentials.Commands
{
    internal class Info : Command
    {
        internal override void init(ICoreServerAPI api)
        {
            api.RegisterCommand("info", "zeigt die Serverinfos und wichtige Commands", "",
                (IServerPlayer player, int groupId, CmdArgs args) =>
                {
                    for (int i = 0; i < CBSEssentials.config.infoMessages.Count; i++)
                    {
                        player.SendMessage(GlobalConstants.GeneralChatGroup, CBSEssentials.config.infoMessages[i], EnumChatType.Notification);
                    }
                }, Privilege.chat);
        }
    }
}