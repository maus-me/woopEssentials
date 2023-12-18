using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace Th3Essentials.Commands;

internal class Message : Command
{
    private readonly Dictionary<string, string> _lastMsgFrom = new();
    private ICoreServerAPI _sapi = null!;

    internal override void Init(ICoreServerAPI sapi)
    {
        _sapi = sapi;
        if (!Th3Essentials.Config.MessageEnabled) return;
        _sapi.ChatCommands.Create("msg")
            .WithDescription(Lang.Get("th3essentials:cd-msg"))
            .RequiresPrivilege(Privilege.chat)
            .WithArgs(_sapi.ChatCommands.Parsers.OnlinePlayer("player"),
                _sapi.ChatCommands.Parsers.All("message"))
            .HandleWith(OnMsgCmd)
            .Validate();

        _sapi.ChatCommands.Create("r")
            .WithDescription(Lang.Get("th3essentials:cd-reply"))
            .RequiresPrivilege(Privilege.chat)
            .WithArgs(_sapi.ChatCommands.Parsers.All("message"))
            .HandleWith(OnRCmd)
            .Validate();
    }

    private TextCommandResult OnRCmd(TextCommandCallingArgs args)
    {
        var player = (IServerPlayer)args.Caller.Player;
        var msgRaw = (string)args.Parsers[0].GetValue();
        if (msgRaw == string.Empty)
        {
            return TextCommandResult.Error(Lang.Get("th3essentials:cd-reply-param"));
        }

        msgRaw = msgRaw.Replace("<", "&lt;").Replace(">", "&gt;");
        var msg =
            $"<font color=\"#{Th3Essentials.Config.MessageCmdColor}\"><strong>{player.PlayerName} whispers:</strong></font> {msgRaw}";
        if (!_lastMsgFrom.TryGetValue(player.PlayerUID, out var otherPlayerUid))
        {
            return TextCommandResult.Error(Lang.Get("th3essentials:cd-reply-fail"));
        }

        var otherPlayers = _sapi.Server.Players.Where((curPlayer) =>
            curPlayer.ConnectionState == EnumClientState.Playing &&
            curPlayer.PlayerUID.Equals(otherPlayerUid));
        var serverPlayers = otherPlayers as IServerPlayer[] ?? otherPlayers.ToArray();
        if (serverPlayers.Length != 1)
        {
            return TextCommandResult.Error(Lang.Get("th3essentials:cd-reply-fail"));
        }

        var otherPlayer = serverPlayers.First();
        var msgSelf =
            $"<font color=\"#{Th3Essentials.Config.MessageCmdColor}\"><strong>whispering to {otherPlayer.PlayerName}:</strong></font> {msgRaw}";

        otherPlayer.SendMessage(GlobalConstants.GeneralChatGroup, msg,
            EnumChatType.OthersMessage);
            
        _sapi.Logger.Chat($"{player.PlayerName} -> {otherPlayer.PlayerName}: {msg}");

        _lastMsgFrom[otherPlayer.PlayerUID] = player.PlayerUID;

        return TextCommandResult.Success(msgSelf);
    }

    private TextCommandResult OnMsgCmd(TextCommandCallingArgs args)
    {
        var player = (IServerPlayer)args.Caller.Player;

        var playername = (IPlayer)args.Parsers[0].GetValue();
        var msgRaw = (string)args.Parsers[1].GetValue();
        if (playername == null || msgRaw == string.Empty)
        {
            return TextCommandResult.Error("/msg " + Lang.Get("th3essentials:cd-msg-param"));
        }

        msgRaw = msgRaw.Replace("<", "&lt;").Replace(">", "&gt;");
        var msg =
            $"<font color=\"#{Th3Essentials.Config.MessageCmdColor}\"><strong>{player.PlayerName} whispers:</strong></font> {msgRaw}";

        var otherPlayers = _sapi.Server.Players.Where((curPlayer) =>
            curPlayer.ConnectionState == EnumClientState.Playing &&
            curPlayer.PlayerName.Equals(playername.PlayerName, StringComparison.InvariantCultureIgnoreCase)).ToList();
        switch (otherPlayers.LongCount())
        {
            case 0:
            {
                return TextCommandResult.Error(Lang.Get("th3essentials:cd-msg-fail", playername));
            }
            case 1:
            {
                var otherPlayer = otherPlayers.First();

                _lastMsgFrom[otherPlayer.PlayerUID] = player.PlayerUID;
                _sapi.Logger.Chat($"{player.PlayerName} -> {otherPlayer.PlayerName}: {msg}");

                otherPlayer.SendMessage(GlobalConstants.GeneralChatGroup, msg, EnumChatType.OthersMessage);
                var msgSelf =
                    $"<font color=\"#{Th3Essentials.Config.MessageCmdColor}\"><strong>whispering to {otherPlayer.PlayerName}:</strong></font> {msgRaw}";

                return TextCommandResult.Success(msgSelf);
            }
            default:
            {
                return TextCommandResult.Error(Lang.Get("th3essentials:cd-msg-fail-mult", playername));
            }
        }
    }
}