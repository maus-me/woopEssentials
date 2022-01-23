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

        private readonly Dictionary<string, string> LastMsgFrom = new Dictionary<string, string>();

        internal override void Init(ICoreServerAPI sapi)
        {
            if (Th3Essentials.Config.MessageEnabled)
            {
                _ = sapi.RegisterCommand("msg", Lang.Get("th3essentials:cd-msg"), Lang.Get("th3essentials:cd-msg-param"),
                    (IServerPlayer player, int groupId, CmdArgs args) =>
                    {
                        string playername = args.PopWord();
                        string msgRaw = args.PopAll();
                        if (playername != null && msgRaw != string.Empty)
                        {
                            msgRaw = msgRaw.Replace("<", "&lt;").Replace(">", "&gt;");
                            string msg = $"<font color=\"#{Th3Essentials.Config.MessageCmdColor}\"><strong>{player.PlayerName} whispers:</strong></font> {msgRaw}";

                            IEnumerable<IServerPlayer> otherPlayers = sapi.Server.Players.Where((curPlayer) => curPlayer.ConnectionState == EnumClientState.Playing && curPlayer.PlayerName.Equals(playername, StringComparison.InvariantCultureIgnoreCase));
                            switch (otherPlayers.LongCount())
                            {
                                case 0:
                                    {
                                        player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("th3essentials:cd-msg-fail", playername), EnumChatType.CommandError);
                                        break;
                                    }
                                case 1:
                                    {
                                        IServerPlayer otherPlayer = otherPlayers.First();

                                        if (LastMsgFrom.TryGetValue(otherPlayer.PlayerUID, out string playeruid))
                                        {
                                            playeruid = player.PlayerUID;
                                        }
                                        else
                                        {
                                            LastMsgFrom.Add(otherPlayer.PlayerUID, player.PlayerUID);
                                        }

                                        string msgSelf = $"<font color=\"#{Th3Essentials.Config.MessageCmdColor}\"><strong>whispering to {otherPlayer.PlayerName}:</strong></font> {msgRaw}";
                                        player.SendMessage(GlobalConstants.GeneralChatGroup, msgSelf, EnumChatType.OwnMessage);

                                        otherPlayer.SendMessage(GlobalConstants.GeneralChatGroup, msg, EnumChatType.OthersMessage);
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

                _ = sapi.RegisterCommand("r", Lang.Get("th3essentials:cd-reply"), Lang.Get("th3essentials:cd-reply-param"),
                (IServerPlayer player, int groupId, CmdArgs args) =>
                {
                    string msgRaw = args.PopAll();
                    if (msgRaw != string.Empty)
                    {
                        msgRaw = msgRaw.Replace("<", "&lt;").Replace(">", "&gt;");
                        string msg = $"<font color=\"#{Th3Essentials.Config.MessageCmdColor}\"><strong>{player.PlayerName} whispers:</strong></font> {msgRaw}";
                        if (LastMsgFrom.TryGetValue(player.PlayerUID, out string otherPlayerUID))
                        {
                            IEnumerable<IServerPlayer> otherPlayers = sapi.Server.Players.Where((curPlayer) => curPlayer.ConnectionState == EnumClientState.Playing && curPlayer.PlayerUID.Equals(otherPlayerUID));
                            if (otherPlayers.Count() == 1)
                            {
                                IServerPlayer otherPlayer = otherPlayers.First();
                                string msgSelf = $"<font color=\"#{Th3Essentials.Config.MessageCmdColor}\"><strong>whispering to {otherPlayer.PlayerName}:</strong></font> {msgRaw}";
                                player.SendMessage(GlobalConstants.GeneralChatGroup, msgSelf, EnumChatType.OwnMessage);

                                otherPlayer.SendMessage(GlobalConstants.GeneralChatGroup, msg, EnumChatType.OthersMessage);
                                if (LastMsgFrom.TryGetValue(otherPlayer.PlayerUID, out string playeruid))
                                {
                                    playeruid = player.PlayerUID;
                                }
                                else
                                {
                                    LastMsgFrom.Add(otherPlayer.PlayerUID, player.PlayerUID);
                                }
                            }
                            else
                            {
                                player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("th3essentials:cd-reply-fail"), EnumChatType.CommandError);
                            }
                        }
                        else
                        {
                            player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("th3essentials:cd-reply-fail"), EnumChatType.CommandError);
                        }
                    }
                    else
                    {
                        player.SendMessage(GlobalConstants.GeneralChatGroup, "/r " + Lang.Get("th3essentials:cd-reply-param"), EnumChatType.CommandError);
                    }
                }, Privilege.chat);
            }
        }
    }
}