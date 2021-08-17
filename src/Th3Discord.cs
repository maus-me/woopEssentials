using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using Th3Essentials.Config;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;

namespace Th3Essentials.Discord
{
    public class Th3Discord
    {
        private DiscordSocketClient _client;

        private IMessageChannel _discordChannel;

        private ICoreServerAPI _api;

        private Th3Config _config;

        private bool initialized;

        public Th3Discord()
        {
            initialized = false;
        }

        public void Init(ICoreServerAPI api)
        {
            _config = Th3Essentials.Config;
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

            // get the channel to send messages from and to
            _discordChannel = _client.GetChannel(_config.ChannelId) as IMessageChannel;
            if (_discordChannel == null)
            {
                _api.Server.LogError($"Could not find channel with id: {_config.ChannelId}");
            }

            // needed since discord might disconect from the gateway and reconnect emitting the ReadyAsync again
            if (!initialized)
            {
                _client.MessageReceived += MessageReceivedAsync;

                //add vs api events
                _api.Event.PlayerChat += PlayerChatAsync;
                _api.Event.PlayerDisconnect += PlayerDisconnectAsync;
                _api.Event.PlayerNowPlaying += PlayerNowPlayingAsync;
                _api.Event.PlayerDeath += PlayerDeathAsync;
                _api.Event.ServerRunPhase(EnumServerRunPhase.GameReady, GameReady);
                _api.Event.ServerRunPhase(EnumServerRunPhase.Shutdown, Shutdown);


                _client.InteractionCreated += InteractionCreated;

                initialized = true;
            }


            UpdatePlayers();
            return Task.CompletedTask;
        }

        private void CreateSlashCommands()
        {
            try
            {
                SlashCommandBuilder players = new SlashCommandBuilder
                {
                    Name = "players",
                    Description = Lang.Get("th3essentials:slc-players")
                };
                _client.Rest.CreateGuildCommand(players.Build(), _config.GuildId);

                SlashCommandBuilder date = new SlashCommandBuilder
                {
                    Name = "date",
                    Description = Lang.Get("th3essentials:slc-date")
                };
                _client.Rest.CreateGuildCommand(date.Build(), _config.GuildId);

                SlashCommandBuilder restart = new SlashCommandBuilder
                {
                    Name = "restart",
                    Description = Lang.Get("th3essentials:slc-restart")
                };
                _client.Rest.CreateGuildCommand(restart.Build(), _config.GuildId);
            }
            catch (ApplicationCommandException exception)
            {
                _api.Logger.VerboseDebug(exception.ToString());
            }
        }

        private Task InteractionCreated(SocketInteraction interaction)
        {
            switch (interaction)
            {
                // Slash commands
                case SocketSlashCommand commandInteraction:
                    {
                        string response;
                        switch (commandInteraction.Data.Name)
                        {
                            case "players":
                                {
                                    List<string> names = new List<string>();
                                    foreach (IServerPlayer player in _api.World.AllOnlinePlayers)
                                    {
                                        names.Add(player.PlayerName);
                                    }
                                    if (names.Count == 0)
                                    {
                                        response = Lang.Get("th3essentials:slc-players-none");
                                    }
                                    else
                                    {
                                        response = string.Join("\n", names);
                                    }
                                    break;
                                }
                            case "date":
                                {
                                    response = _api.World.Calendar.PrettyDate();
                                    break;
                                }
                            case "restart":
                                {
                                    TimeSpan restart = Th3Util.GetTimeTillRestart();
                                    response = Lang.Get("th3essentials:slc-restart-resp", restart.Hours.ToString("D2"), restart.Minutes.ToString("D2"));
                                    break;
                                }
                            default:
                                {
                                    response = "Unknown SlashCommand";
                                    break;
                                }
                        }
                        commandInteraction.RespondAsync(ServerMsg(response));
                        break;
                    }
            }
            return Task.CompletedTask;
        }

        internal void SendMessage(string msg)
        {
            _discordChannel.SendMessageAsync(ServerMsg(msg));
        }

        private void PlayerDeathAsync(IServerPlayer byPlayer, DamageSource damageSource)
        {
            if (_discordChannel != null)
            {
                string msg;
                if (damageSource != null)
                {
                    string key = null;
                    int numMax = 1;
                    if (damageSource.SourceEntity != null)
                    {
                        key = damageSource.SourceEntity.Code.Path.Replace("-", "");
                        if (key.Contains("wolf"))
                        {
                            numMax = 4;
                        }
                        else if (key.Contains("pig"))
                        {
                            numMax = 1;
                        }
                        else if (key.Contains("drifter"))
                        {
                            numMax = 3;
                        }
                        else if (key.Contains("sheep"))
                        {
                            if (key.Contains("female"))
                            {
                                key = "sheepbighornmale";
                            }
                            numMax = 3;
                        }
                        else if (key.Contains("locust"))
                        {
                            numMax = 2;
                        }
                    }
                    else
                    {
                        if (damageSource.Source == EnumDamageSource.Explosion)
                        {
                            key = "explosion";
                            numMax = 4;
                        }
                        else if (damageSource.Type == EnumDamageType.Hunger)
                        {
                            key = "hunger";
                            numMax = 3;
                        }
                        else if (damageSource.Type == EnumDamageType.Fire)
                        {
                            key = "fire-block";
                            numMax = 3;
                        }
                        else if (damageSource.Source == EnumDamageSource.Fall)
                        {
                            key = "fall";
                            numMax = 4;
                        }
                    }

                    if (key != null)
                    {
                        Random rnd = new Random();

                        msg = Lang.Get("deathmsg-" + key + "-" + rnd.Next(1, numMax), byPlayer.PlayerName);
                        if (msg.Contains("deathmsg"))
                        {
                            string str = Lang.Get("prefixandcreature-" + key);
                            msg = Lang.Get("th3essentials:playerdeathby", byPlayer.PlayerName, str);
                        }
                    }
                    else
                    {
                        msg = Lang.Get("th3essentials:playerdeath", byPlayer.PlayerName);
                    }
                }
                else
                {
                    msg = Lang.Get("th3essentials:playerdeath", byPlayer.PlayerName);
                }
                _discordChannel.SendMessageAsync(ServerMsg(msg));
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

            if (message.Content.ToLower().StartsWith("!reloadcommands"))
            {
                if (message.Author is SocketGuildUser guildUser)
                {
                    if (guildUser.GuildPermissions.Administrator)
                    {
                        CreateSlashCommands();
                        message.ReplyAsync("reloadcommands executed");
                    }
                }
            }
            // only send messages from specific channel to vs server chat
            // ignore empty messages (message is empty when only picture/file is send)
            else if (message.Channel.Id == _config.ChannelId && message.Content != "")
            {
                string msg = CleanDiscordMessage(message);
                // use blue font ingame for discord messages
                const string format = "<font color=\"#7289DA\"><strong>{0}:</strong></font> {1}";
                if (message.Author is SocketGuildUser guildUser)
                {
                    string name = guildUser.Nickname ?? guildUser.Username;
                    msg = message.Attachments.Count > 0
                        ? string.Format(format, name, $" [Attachments] {msg}")
                        : string.Format(format, name, msg);
                    _api.SendMessageToGroup(GlobalConstants.GeneralChatGroup, msg, EnumChatType.OthersMessage);
                }
            }
            return Task.CompletedTask;
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
                    if (mUser is SocketGuildUser mGuildUser)
                    {
                        if (mGuildUser.Id.ToString() == user.Groups[1].Value)
                        {
                            string name = mGuildUser.Nickname ?? mGuildUser.Username;
                            msg = Regex.Replace(msg, $"<@!?{user.Groups[1].Value}>", $"@{name}");
                            break;
                        }
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
            if (_discordChannel != null)
            {
                Match playerMsg = Regex.Match(message, "> (.+)");
                string msg = playerMsg.Groups[1].Value;
                msg = msg.Replace("&lt;", "<").Replace("&gt;", ">");
                msg = string.Format("**{0}:** {1}", byPlayer.PlayerName, msg);
                _discordChannel.SendMessageAsync(msg);
            }
        }

        private void PlayerDisconnectAsync(IServerPlayer byPlayer)
        {
            // update player count with allplayer -1 since the disconnecting player is still online while this event fires
            UpdatePlayers(_api.World.AllOnlinePlayers.Length - 1);
            if (_discordChannel != null)
            {
                string msg = Lang.Get("th3essentials:disconnected", byPlayer.PlayerName);
                _discordChannel.SendMessageAsync(ServerMsg(msg));
            }
        }

        private void PlayerNowPlayingAsync(IServerPlayer byPlayer)
        {
            UpdatePlayers();
            if (_discordChannel != null)
            {
                string msg = Lang.Get("th3essentials:connected", byPlayer.PlayerName);
                _discordChannel.SendMessageAsync(ServerMsg(msg));
            }
        }

        private void UpdatePlayers(int players = -1)
        {
            if (players < 0)
            {
                players = _api.World.AllOnlinePlayers.Length;
            }
            _client.SetGameAsync($"players: {players}");
        }

        private void GameReady()
        {
            if (_discordChannel != null)
            {
                _discordChannel.SendMessageAsync(ServerMsg(Lang.Get("th3essentials:start")));
            }
        }

        private async void Shutdown()
        {
            if (_client != null)
            {
                if (_discordChannel != null)
                {
                    await _discordChannel.SendMessageAsync(ServerMsg(Lang.Get("th3essentials:shutdown")));
                }
                _client.Dispose();
            }
        }

        private string ServerMsg(string msg)
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