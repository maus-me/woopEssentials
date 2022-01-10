using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Th3Essentials.Config;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Th3Essentials.Discordbot
{
  public class Th3Discord
  {
    internal DiscordSocketClient _client;

    internal IMessageChannel _discordChannel;

    internal ICoreServerAPI _api;

    internal Th3DiscordConfig _config;

    private bool initialized;

    public Th3Discord()
    {
      initialized = false;
    }

    public void Init(ICoreServerAPI api)
    {
      _config = Th3Essentials.Config.DiscordConfig;
      _api = api;

      // create Discord client and set event methodes
      _client = new DiscordSocketClient();

      _client.Ready += ReadyAsync;
      _client.Log += LogAsync;

      _api.Server.LogVerboseDebug("Discord started");

      // start discord bot
      BotMainAsync();
    }

    public async void BotMainAsync()
    {
      await _client.LoginAsync(TokenType.Bot, _config.Token);
      await _client.StartAsync();

      // keep the discord bot thread running
      await Task.Delay(Timeout.Infinite);
    }

    private Task ReadyAsync()
    {
      _api.Server.LogVerboseDebug($"{_client.CurrentUser} is connected!");

      if (!GetDiscordChannel())
      {
        _api.Server.LogError($"Could not find channel with id: {_config.ChannelId}");
      }

      // needed since discord might disconect from the gateway and reconnect emitting the ReadyAsync again
      if (!initialized)
      {
        _client.MessageReceived += MessageReceivedAsync;
        _client.InteractionCreated += InteractionCreated;
        _client.ButtonExecuted += ButtonExecuted;

        //add vs api events
        _api.Event.PlayerChat += PlayerChatAsync;
        _api.Event.PlayerDisconnect += PlayerDisconnectAsync;
        _api.Event.PlayerNowPlaying += PlayerNowPlayingAsync;
        _api.Event.ServerRunPhase(EnumServerRunPhase.GameReady, GameReady);
        _api.Event.ServerRunPhase(EnumServerRunPhase.Shutdown, Shutdown);

        initialized = true;
      }

      UpdatePlayers();
      return Task.CompletedTask;
    }

    private void CreateSlashCommands()
    {
      DeleteCommands();
      try
      {
        Th3SlashCommands.CreateGuildCommands(_client);
      }
      catch (Exception exception)
      {
        _api.Logger.Error("Slashcommand create:" + exception.ToString());
        _api.Logger.Error("Maybe you forgot to add the applications.commands for your bot");
      }
    }

    private async void DeleteCommands()
    {
      try
      {
        IReadOnlyCollection<RestGuildCommand> commands = await _client.Rest.GetGuildApplicationCommands(_config.GuildId);
        foreach (RestGuildCommand cmd in commands)
        {
          string[] cmds = Enum.GetNames(typeof(SlashCommands)).Select(scmd => scmd.ToLower()).ToArray();
          if (!cmds.Contains(cmd.Name))
          {
            await cmd.DeleteAsync();
          }
        }
      }
      catch (Exception exception)
      {
        _api.Logger.Error("Slashcommand delete:" + exception.ToString());
        _api.Logger.Error("Maybe you forgot to add the applications.commands for your bot");
      }
    }

    private Task InteractionCreated(SocketInteraction interaction)
    {
      switch (interaction)
      {
        // Slash commands
        case SocketSlashCommand commandInteraction:
          {
            Th3SlashCommands.HandleSlashCommand(this, commandInteraction);
            break;
          }
        default:
          break;
      }
      return Task.CompletedTask;
    }



    private Task ButtonExecuted(SocketMessageComponent component)
    {
      Th3SlashCommands.HandleButtonExecuted(this, component);
      return Task.CompletedTask;
    }

    internal void SendServerMessage(string msg)
    {
      if (_discordChannel != null)
      {
        _ = _discordChannel.SendMessageAsync(ServerMsg(msg));
      }
    }
    private Task MessageReceivedAsync(SocketMessage messageParam)
    {
      if (messageParam.Author.IsBot)
      {
        return Task.CompletedTask;
      }
      // check if message is from a user else do nothing
      if (!(messageParam is SocketUserMessage message))
      {
        return Task.CompletedTask;
      }

      if (message.Content.ToLower().StartsWith("!setupth3essentials"))
      {
        if (message.Author is SocketGuildUser guildUser)
        {
          if (guildUser.GuildPermissions.Administrator)
          {
            _config.GuildId = guildUser.Guild.Id;
            _config.ChannelId = message.Channel.Id;

            CreateSlashCommands();
            if (!GetDiscordChannel())
            {
              _api.Server.LogError($"Could not find channel with id: {_config.ChannelId}");
              _ = message.ReplyAsync($"Could not find channel with id: {_config.ChannelId}");
            }
            else
            {
              _ = message.ReplyAsync("Th3Essentials: Commands, Guild and Channel are setup :thumbsup:");
            }
          }
        }
      }
      // only send messages from specific channel to vs server chat
      // ignore empty messages (message is empty when only picture/file is send)
      else if (message.Channel.Id == _config.ChannelId && message.Content != "")
      {
        string msg = CleanDiscordMessage(message);
        // use blue font ingame for discord messages
        // const string format = "<font color=\"#{0}\"><strong>{1}: </strong></font><font family=\"Twitter Color Emoji\">{2}</font>";
        const string format = "<font color=\"#{0}\"><strong>{1}: </strong></font>{2}";
        if (message.Author is SocketGuildUser guildUser)
        {
          string name = guildUser.Nickname ?? guildUser.Username;
          msg = message.Attachments.Count > 0
              ? string.Format(format, _config.DiscordChatColor, name, $" [Attachments] {msg}")
              : string.Format(format, _config.DiscordChatColor, name, msg);
          _api.SendMessageToGroup(GlobalConstants.GeneralChatGroup, msg, EnumChatType.OthersMessage);
          _api.Logger.Chat(msg);
        }
      }
      return Task.CompletedTask;
    }

    internal bool GetDiscordChannel()
    {
      _discordChannel = _client.GetChannel(_config.ChannelId) as IMessageChannel;
      return _discordChannel != null;
    }

    private string CleanDiscordMessage(SocketMessage message)
    {
      string msg = message.Content;
      // find user id (https://discord.com/developers/docs/reference#message-formatting)
      MatchCollection userMention = Regex.Matches(msg, "<@!?([0-9]+)>");
      foreach (Match user in userMention)
      {
        foreach (SocketUser mUser in message.MentionedUsers)
        {
          if (mUser.Id.ToString() == user.Groups[1].Value)
          {
            string name = "Unknown";
            if (mUser is SocketGuildUser mGuildUser)
            {
              name = mGuildUser.Nickname ?? mGuildUser.Username;
            }
            else if (mUser is SocketUnknownUser)
            {
              RestGuildUser rgUser = _client.Rest.GetGuildUserAsync(_config.GuildId, mUser.Id).GetAwaiter().GetResult();
              name = rgUser.Nickname ?? rgUser.Username;
            }
            msg = Regex.Replace(msg, $"<@!?{user.Groups[1].Value}>", $"@{name}");
            break;
          }
        }
      }

      MatchCollection roleMention = Regex.Matches(msg, "<@&([0-9]+)>");
      foreach (Match role in roleMention)
      {
        foreach (SocketRole mRole in message.MentionedRoles)
        {
          if (mRole.Id.ToString() == role.Groups[1].Value)
          {
            msg = msg.Replace($"<@&{role.Groups[1].Value}>", $"@{mRole.Name}");
            break;
          }
        }
      }

      MatchCollection channelMention = Regex.Matches(msg, "<#([0-9]+)>");
      foreach (Match channel in channelMention)
      {
        foreach (SocketChannel mChannel in message.MentionedChannels)
        {
          if (mChannel.Id.ToString() == channel.Groups[1].Value)
          {
            msg = msg.Replace($"<#{channel.Groups[1].Value}>", $"#{mChannel}");
            break;
          }
        }
      }
      return msg.Replace("<", "&lt;").Replace(">", "&gt;");
    }

    private void PlayerChatAsync(IServerPlayer byPlayer, int channelId, ref string message, ref string data, BoolRef consumed)
    {
      if (_discordChannel != null && channelId == GlobalConstants.GeneralChatGroup)
      {
        Match playerMsg = Regex.Match(message, "> (.+)");
        string msg = playerMsg.Groups[1].Value;
        msg = msg.Replace("&lt;", "<").Replace("&gt;", ">");
        msg = string.Format("**{0}:** {1}", byPlayer.PlayerName, msg);
        _ = _discordChannel.SendMessageAsync(msg);
      }
    }

    private void PlayerDisconnectAsync(IServerPlayer byPlayer)
    {
      // update player count with allplayer -1 since the disconnecting player is still online while this event fires
      int players = UpdatePlayers(_api.World.AllOnlinePlayers.Length - 1);

      SendServerMessage(Lang.Get("th3essentials:disconnected", byPlayer.PlayerName, players, _api.Server.Config.MaxClients));
    }

    private void PlayerNowPlayingAsync(IServerPlayer byPlayer)
    {
      int players = UpdatePlayers();

      SendServerMessage(Lang.Get("th3essentials:connected", byPlayer.PlayerName, players, _api.Server.Config.MaxClients));
    }

    private int UpdatePlayers(int players = -1)
    {
      if (players < 0)
      {
        players = _api.World.AllOnlinePlayers.Length;
      }
      _ = _client.SetGameAsync($"players: {players}");
      return players;
    }

    private void GameReady()
    {
      SendServerMessage(Lang.Get("th3essentials:start"));
    }

    private async void Shutdown()
    {
      if (_client != null)
      {
        if (_discordChannel != null)
        {
          _ = await _discordChannel.SendMessageAsync(ServerMsg(Lang.Get("th3essentials:shutdown")));
        }
        _client.Dispose();
      }
    }

    internal string ServerMsg(string msg)
    {
      return $"*{msg}*";
    }

    private Task LogAsync(LogMessage log)
    {
      _api.Server.LogVerboseDebug($"[Discord] {log.ToString()}");
      return Task.CompletedTask;
    }
  }
}