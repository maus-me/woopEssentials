using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace Th3Essentials.Commands
{
  internal class Serverinfo : Command
  {
    internal override void Init(ICoreServerAPI api)
    {
      if (Th3Essentials.Config.InfoMessage != null)
      {
        api.RegisterCommand("serverinfo", Lang.Get("th3essentials:cd-info"), string.Empty,
            (IServerPlayer player, int groupId, CmdArgs args) =>
            {
              player.SendMessage(GlobalConstants.GeneralChatGroup, Th3Essentials.Config.InfoMessage, EnumChatType.Notification);
            }, Privilege.chat);
      }
    }
  }
}