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
                    string msg = args.PopAll();
                    if (playername != null && msg != string.Empty)
                    {
                        msg = msg.Replace("<", "&lt;").Replace(">", "&gt;");
                        msg = $"<font color=\"{Th3Essentials.Config.CmdMessageColor}\"><strong>{player.PlayerName}:</strong></font> {msg}";

                        IEnumerable<IServerPlayer> otherPlayers = api.Server.Players.Where((curPlayer) => curPlayer.ConnectionState == EnumClientState.Playing && curPlayer.PlayerName.Equals(playername, StringComparison.InvariantCultureIgnoreCase));
                        switch (otherPlayers.LongCount())
                        {
                            case 0:
                                {
                                    player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("th3essentials:cd-msg-fail", playername), EnumChatType.CommandError);
                                    break;
                                }
                            case 1:
                                {
                                    otherPlayers.First().SendMessage(GlobalConstants.GeneralChatGroup, msg, EnumChatType.OthersMessage);
                                    player.SendMessage(GlobalConstants.GeneralChatGroup, msg, EnumChatType.OwnMessage);
                                    break;
                                }
                            default:
                                {
                                    player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("th3essentials:cd-msg-fail-mult", playername), EnumChatType.CommandError);
                                    break;
                                }
                        }
                    }
                    else
                    {
                        player.SendMessage(GlobalConstants.GeneralChatGroup, "/msg " + Lang.Get("th3essentials:cd-msg-param"), EnumChatType.CommandError);
                    }
                }, Privilege.chat);
        }
    }
}