using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using WoopEssentials.Config;

namespace WoopEssentials.Commands;

internal class Message : Command
{
    private readonly Dictionary<string, string> _lastMsgFrom = new();
    private ICoreServerAPI _sapi = null!;

    internal override void Init(ICoreServerAPI sapi)
    {
        _sapi = sapi;
        if (!WoopEssentials.Config.MessageEnabled) return;
        
        // Register player login event to show mail notifications
        _sapi.Event.PlayerNowPlaying += OnPlayerJoin;
        
        _sapi.ChatCommands.Create("msg")
            .WithAlias("whisper")
            .WithDescription(Lang.Get("woopessentials:cd-msg"))
            .RequiresPrivilege(Privilege.chat)
            .WithArgs(_sapi.ChatCommands.Parsers.OnlinePlayer("player"),
                _sapi.ChatCommands.Parsers.All("message"))
            .HandleWith(OnMsgCmd)
            .Validate();

        _sapi.ChatCommands.Create("r")
            .WithAlias("reply")
            .WithDescription(Lang.Get("woopessentials:cd-reply"))
            .RequiresPrivilege(Privilege.chat)
            .WithArgs(_sapi.ChatCommands.Parsers.All("message"))
            .HandleWith(OnRCmd)
            .Validate();

        _sapi.ChatCommands.Create("mail")
            .WithDescription(Lang.Get("woopessentials:cd-mail"))
            .RequiresPrivilege(Privilege.chat)
            .BeginSubCommand("read")
                .WithDescription(Lang.Get("woopessentials:cd-mail-read"))
                .IgnoreAdditionalArgs()
                .WithAlias("inbox")
                .RequiresPrivilege(Privilege.chat)
                .HandleWith(OnMailRead)
            .EndSubCommand()
            .BeginSubCommand("send")
                .WithDescription(Lang.Get("woopessentials:cd-mail-send"))
                .RequiresPrivilege(Privilege.chat)
                .WithArgs(_sapi.ChatCommands.Parsers.PlayerUids("player"),
                    _sapi.ChatCommands.Parsers.All("message"))
                .HandleWith(OnMailSend)
            .EndSubCommand()
            .BeginSubCommand("clear")
                .WithDescription(Lang.Get("woopessentials:cd-mail-clear"))
                .IgnoreAdditionalArgs()
                .WithAlias("delete")
                .RequiresPrivilege(Privilege.chat)
                .HandleWith(OnMailClear)
            .EndSubCommand()
            .Validate();
    }
    
    private void OnPlayerJoin(IServerPlayer player)
    {
        // Get player data
        var playerData = WoopEssentials.PlayerConfig.GetPlayerDataByUid(player.PlayerUID);
        
        // Check for unread mail
        if (playerData.Mails.Count > 0)
        {
            int unreadCount = playerData.Mails.Count(m => !m.IsRead);
            
            if (unreadCount > 0)
            {
                // Notify player about unread mail
                string notification = $"<font color=\"#{WoopEssentials.Config.MessageCmdColor}\"><strong>{Lang.Get("woopessentials:mail-new-notification", unreadCount, unreadCount == 1 ? "" : "s")}</strong></font>";
                player.SendMessage(GlobalConstants.GeneralChatGroup, notification, EnumChatType.Notification);
                
                try
                {
                    var sound = new AssetLocation("game", "sounds/player/projectilehit");
                    _sapi.World.PlaySoundFor(sound, player, false, 16f, 0.75f);
                }
                catch (Exception e)
                {
                    _sapi.Logger.Warning($"Failed to play mail notification sound for {player.PlayerName}: {e.Message}");
                }
            }
        }
    }

    /* Implements a basic mail system that stores messages for offline players.
      Messages are stored in player data and persist between server restarts. */
    private TextCommandResult OnMailRead(TextCommandCallingArgs args)
    {
        var player = (IServerPlayer)args.Caller.Player;
        
        // Get player data
        var playerData = WoopEssentials.PlayerConfig.GetPlayerDataByUid(player.PlayerUID);
        
        // If no mail, return message
        if (playerData.Mails.Count == 0)
        {
            return TextCommandResult.Success(Lang.Get("woopessentials:mail-no-messages"));
        }
        
        // Build messages display
        var sb = new StringBuilder();
        sb.AppendLine($"<font color=\"#{WoopEssentials.Config.MessageCmdColor}\"><strong>{Lang.Get("woopessentials:mail-inbox-header", playerData.Mails.Count)}</strong></font>");
        
        // Sort messages by time (newest first)
        var sortedMails = playerData.Mails.OrderByDescending(m => m.SentTime).ToList();
        
        for (var i = 0; i < sortedMails.Count; i++)
        {
            var mail = sortedMails[i];
            var timeString = mail.SentTime.ToString("g"); // Short date and time pattern
            var readStatus = mail.IsRead ? "[Read]" : "[New]";
            
            sb.AppendLine($"{i+1}. {readStatus} <strong>From:</strong> {mail.SenderName} <strong>Date:</strong> {timeString}");
            sb.AppendLine($"   <strong>Message:</strong> {mail.Message}");
            sb.AppendLine();
            
            // Mark as read
            if (!mail.IsRead)
            {
                mail.IsRead = true;
                playerData.MarkDirty();
            }
        }
        
        return TextCommandResult.Success(sb.ToString());
    }

    private TextCommandResult OnMailSend(TextCommandCallingArgs args)
    {
        var player = (IServerPlayer)args.Caller.Player;
        var recipient = (PlayerUidName[])args.Parsers[0].GetValue();
        var msgRaw = (string)args.Parsers[1].GetValue();
        
        if (recipient == null || recipient.Length == 0 || string.IsNullOrEmpty(msgRaw))
        {
            return TextCommandResult.Error(Lang.Get("woopessentials:mail-usage-send"));
        }
        
        // Get the recipient data from the array's first element
        var targetUid = recipient[0].Uid;
        var targetName = recipient[0].Name;
        
        // Validate that the player exists
        var playerData = _sapi.PlayerData.GetPlayerDataByUid(targetUid);
        if (playerData == null)
        {
            // Try to look up by name as a fallback
            var playerByName = _sapi.PlayerData.GetPlayerDataByLastKnownName(targetName);
            if (playerByName == null)
            {
                return TextCommandResult.Error(Lang.Get("woopessentials:mail-invalid-player"));
            }
            // Update target UID if found by name
            targetUid = playerByName.PlayerUID;
        }
        
        // Sanitize message
        msgRaw = msgRaw.Replace("<", "&lt;").Replace(">", "&gt;");
        
        // Create mail message
        var mail = new Mail(player.PlayerName, player.PlayerUID, msgRaw);
        
        // Get or create player data for target
        var recipientData = WoopEssentials.PlayerConfig.GetPlayerDataByUid(targetUid);
        
        // Add mail to player's inbox
        recipientData.Mails.Add(mail);
        recipientData.MarkDirty();
        
        // If player is online, notify them
        var onlinePlayer = _sapi.World.PlayerByUid(targetUid) as IServerPlayer;
        if (onlinePlayer != null && onlinePlayer.ConnectionState == EnumClientState.Playing)
        {
            var count = recipientData.Mails.Count(m => !m.IsRead);
            var notification = $"<font color=\"#{WoopEssentials.Config.MessageCmdColor}\"><strong>{Lang.Get("woopessentials:mail-new-notification", count, "")}</strong></font>";
            onlinePlayer.SendMessage(GlobalConstants.GeneralChatGroup, notification, EnumChatType.Notification);
            
            try
            {
                var sound = new AssetLocation("game", "sounds/player/projectilehit");
                _sapi.World.PlaySoundFor(sound, onlinePlayer, false, 16f, 0.75f);
            }
            catch (Exception e)
            {
                _sapi.Logger.Warning($"Failed to play mail notification sound for {onlinePlayer.PlayerName}: {e.Message}");
            }
        }
        
        // Log the mail
        _sapi.Logger.Chat($"{player.PlayerName} sent mail to {targetName}: {msgRaw}");
        
        // Return success message
        return TextCommandResult.Success(Lang.Get("woopessentials:mail-sent", targetName));
    }

    private static TextCommandResult OnMailClear(TextCommandCallingArgs args)
    {
        var player = (IServerPlayer)args.Caller.Player;
        
        // Get player data
        var playerData = WoopEssentials.PlayerConfig.GetPlayerDataByUid(player.PlayerUID);
        
        // If no mail, return message
        if (playerData.Mails.Count == 0)
        {
            return TextCommandResult.Success(Lang.Get("woopessentials:mail-no-clear"));
        }
        
        // Clear mail
        int mailCount = playerData.Mails.Count;
        playerData.Mails.Clear();
        playerData.MarkDirty();
        
        return TextCommandResult.Success(Lang.Get("woopessentials:mail-cleared", mailCount, mailCount == 1 ? "" : "s"));
    }

    private TextCommandResult OnRCmd(TextCommandCallingArgs args)
    {
        var player = (IServerPlayer)args.Caller.Player;
        var msgRaw = (string)args.Parsers[0].GetValue();
        if (msgRaw == string.Empty)
        {
            return TextCommandResult.Error(Lang.Get("woopessentials:cd-reply-param"));
        }

        msgRaw = msgRaw.Replace("<", "&lt;").Replace(">", "&gt;");
        var msg =
            $"<font color=\"#{WoopEssentials.Config.MessageCmdColor}\"><strong>{player.PlayerName} whispers:</strong></font> {msgRaw}";
        if (!_lastMsgFrom.TryGetValue(player.PlayerUID, out var otherPlayerUid))
        {
            return TextCommandResult.Error(Lang.Get("woopessentials:cd-reply-fail"));
        }

        var otherPlayers = _sapi.Server.Players.Where((curPlayer) =>
            curPlayer.ConnectionState == EnumClientState.Playing &&
            curPlayer.PlayerUID.Equals(otherPlayerUid));
        var serverPlayers = otherPlayers as IServerPlayer[] ?? otherPlayers.ToArray();
        if (serverPlayers.Length != 1)
        {
            return TextCommandResult.Error(Lang.Get("woopessentials:cd-reply-fail"));
        }

        var otherPlayer = serverPlayers.First();
        var msgSelf =
            $"<font color=\"#{WoopEssentials.Config.MessageCmdColor}\"><strong>whispering to {otherPlayer.PlayerName}:</strong></font> {msgRaw}";

        otherPlayer.SendMessage(GlobalConstants.GeneralChatGroup, msg,
            EnumChatType.OthersMessage);
        // Play sound for recipient (server-side only for the target player)
        try
        {
            // Sound path is relative to assets/sounds without .ogg; using a common UI notify sound
            var sound = new AssetLocation("game","sounds/player/projectilehit");
            _sapi.World.PlaySoundFor(sound, otherPlayer, false, 16f, 0.75f);
        }
        catch (Exception e)
        {
            _sapi.Logger.Warning($"Failed to play pm sound for {otherPlayer.PlayerName}: {e.Message}");
        }

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
            return TextCommandResult.Error("/msg " + Lang.Get("woopessentials:cd-msg-param"));
        }
        // Handle the case where the server console is sending a message
        var senderName = "Console";
        if (player != null)
        {
            senderName = player.PlayerName;
        }

        msgRaw = msgRaw.Replace("<", "&lt;").Replace(">", "&gt;");
        var msg =
            $"<font color=\"#{WoopEssentials.Config.MessageCmdColor}\"><strong>{senderName} whispers:</strong></font> {msgRaw}";

        var otherPlayers = _sapi.Server.Players.Where((curPlayer) =>
            curPlayer.ConnectionState == EnumClientState.Playing &&
            curPlayer.PlayerName.Equals(playername.PlayerName, StringComparison.InvariantCultureIgnoreCase)).ToList();
        switch (otherPlayers.LongCount())
        {
            case 0:
            {
                return TextCommandResult.Error(Lang.Get("woopessentials:cd-msg-fail", playername));
            }
            case 1:
            {
                var otherPlayer = otherPlayers.First();

                // If message was sent by a player, track it as _lastMsgFrom for reply purposes
                if (player != null)
                {
                    _lastMsgFrom[otherPlayer.PlayerUID] = player.PlayerUID;
                }

                _sapi.Logger.Chat($"{senderName} -> {otherPlayer.PlayerName}: {msg}");

                otherPlayer.SendMessage(GlobalConstants.GeneralChatGroup, msg, EnumChatType.OthersMessage);
                try
                {
                    var sound = new AssetLocation("game","sounds/player/projectilehit");
                    _sapi.World.PlaySoundFor(sound, otherPlayer, false, 16f, 0.75f);
                }
                catch (Exception e)
                {
                    _sapi.Logger.Warning($"Failed to play pm sound for {otherPlayer.PlayerName}: {e.Message}");
                }
                var msgSelf =
                    $"<font color=\"#{WoopEssentials.Config.MessageCmdColor}\"><strong>whispering to {otherPlayer.PlayerName}:</strong></font> {msgRaw}";

                return TextCommandResult.Success(msgSelf);
            }
            default:
            {
                return TextCommandResult.Error(Lang.Get("woopessentials:cd-msg-fail-mult", playername));
            }
        }
    }

    /* Todo: Review the previous two functions to see if these can be consolidated.
     A bit confused why the previous two are separate since they appear to largely share the same code. */
}