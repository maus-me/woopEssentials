using System;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace Th3Essentials.Commands
{
    internal class Restart : Command
    {
        internal override void Init(ICoreServerAPI api)
        {
            api.RegisterCommand("restart", Lang.Get("th3essentials:cd-restart"), string.Empty,
                (IServerPlayer player, int groupId, CmdArgs args) =>
                {
                    TimeSpan restart = Th3Util.GetTimeTillRestart();
                    string response = Lang.Get("th3essentials:slc-restart-resp", restart.Hours.ToString("D2"), restart.Minutes.ToString("D2"));
                    player.SendMessage(GlobalConstants.GeneralChatGroup, response, EnumChatType.Notification);
                }, Privilege.chat);
        }
    }
}