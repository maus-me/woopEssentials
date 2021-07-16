using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace Th3Essentials.Commands
{
    internal class Info : Command
    {
        internal override void Init(ICoreServerAPI api)
        {
            api.RegisterCommand("info", Lang.Get("th3essentials:cd-info"), string.Empty,
                (IServerPlayer player, int groupId, CmdArgs args) =>
                {
                    for (int i = 0; i < Th3Essentials.Config.infoMessages.Count; i++)
                    {
                        player.SendMessage(GlobalConstants.GeneralChatGroup, Th3Essentials.Config.infoMessages[i], EnumChatType.Notification);
                    }
                }, Privilege.chat);
        }
    }
}