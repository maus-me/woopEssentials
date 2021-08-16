using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace Th3Essentials.Commands
{
    internal class Message : Command
    {
        internal override void Init(ICoreServerAPI api)
        {
            api.RegisterCommand("msg", Lang.Get("th3essentials:cd-msg"), Lang.Get("th3essentials:cd-msg-param"),
                (IServerPlayer player, int groupId, CmdArgs args) =>
                {
                    string playername = args.PopWord();
                    string msg = args.PopAll().Replace("<", "&lt;").Replace(">", "&gt;");
                    msg = $"<font color=\"#ff9102\"><strong>{player.PlayerName}:</strong></font> {msg}";

                    IEnumerable<IServerPlayer> otherPlayers = api.Server.Players.Where((curPlayer) => curPlayer.PlayerName.Equals(playername, StringComparison.InvariantCultureIgnoreCase));
                    if (otherPlayers.LongCount() == 1)
                    {
                        otherPlayers.First().SendMessage(GlobalConstants.GeneralChatGroup, msg, EnumChatType.OthersMessage);
                        player.SendMessage(GlobalConstants.GeneralChatGroup, msg, EnumChatType.OwnMessage);
                    }
                    else
                    {
                        player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("th3essentials:cd-msg-fail", playername), EnumChatType.CommandError);
                    }
                }, Privilege.chat);

        }
    }
}