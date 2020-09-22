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
                                  player.SendMessage(GlobalConstants.GeneralChatGroup, "--------------------", EnumChatType.Notification);
                                  player.SendMessage(GlobalConstants.GeneralChatGroup, "Dieser Server legt den Fokus auf survival.", EnumChatType.Notification);
                                  player.SendMessage(GlobalConstants.GeneralChatGroup, "<strong>Wichtige Commands:</strong>", EnumChatType.Notification);
                                  player.SendMessage(GlobalConstants.GeneralChatGroup, "/players | Zeigt dir alle Spieler an, die online sind", EnumChatType.Notification);
                                  player.SendMessage(GlobalConstants.GeneralChatGroup, "/spawn | Teleportiert dich zu deinem aktuellen Spawn. Um den Spawn zu setzen verwende ein temporal Gear", EnumChatType.Notification);
                                  player.SendMessage(GlobalConstants.GeneralChatGroup, "/sethome | Setzt einen Punkt zu dem du mit /home teleportieren kannst.", EnumChatType.Notification);
                                  player.SendMessage(GlobalConstants.GeneralChatGroup, "--------------------", EnumChatType.Notification);

                              }, Privilege.chat);
        }
    }
}