using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace CBSEssentials.Commands
{
    internal class Players : Command
    {
        internal override void init(ICoreServerAPI api)
        {
            api.RegisterCommand("players", "shows a list of all online players", "",
                  (IServerPlayer player, int groupId, CmdArgs args) =>
                  {
                      player.SendMessage(GlobalConstants.GeneralChatGroup, "--------------------", EnumChatType.Notification);
                      player.SendMessage(GlobalConstants.GeneralChatGroup, "Online: ", EnumChatType.Notification);

                      IPlayer[] players = api.World.AllOnlinePlayers;

                      for (int i = 0; i < players.Length; i++)
                      {
                          player.SendMessage(GlobalConstants.GeneralChatGroup, $"<strong>{players[i].PlayerName}</strong>", EnumChatType.Notification);
                      }

                      player.SendMessage(GlobalConstants.GeneralChatGroup, "--------------------", EnumChatType.Notification);

                  }, Privilege.chat);
        }

    }
}