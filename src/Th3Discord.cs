using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Net;
using Discord.Rest;
using Discord.WebSocket;
using Th3Essentials.Config;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.Server;

namespace Th3Essentials.Discordbot
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
            _client = new DiscordSocketClient(new DiscordSocketConfig()
            {
                AlwaysAcknowledgeInteractions = false
            });
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

                //add vs api events
                _api.Event.PlayerChat += PlayerChatAsync;
                _api.Event.PlayerDisconnect += PlayerDisconnectAsync;
                _api.Event.PlayerNowPlaying += PlayerNowPlayingAsync;
                _api.Event.PlayerDeath += PlayerDeathAsync;
                _api.Event.ServerRunPhase(EnumServerRunPhase.GameReady, GameReady);
                _api.Event.ServerRunPhase(EnumServerRunPhase.Shutdown, Shutdown);

                initialized = true;
            }


            UpdatePlayers();
            return Task.CompletedTask;
        }

        private void CreateSlashCommands()
        {
            IReadOnlyCollection<RestGuildCommand> commands = _client.Rest.GetGuildApplicationCommands(_config.GuildId).GetAwaiter().GetResult();
            foreach (RestGuildCommand cmd in commands)
            {
                cmd.DeleteAsync();
            }
            try
            {
                SlashCommandBuilder players = new SlashCommandBuilder
                {
                    Name = "players",
                    Description = Lang.Get("th3essentials:slc-players")
                };
                _ = _client.Rest.CreateGuildCommand(players.Build(), _config.GuildId);

                SlashCommandBuilder date = new SlashCommandBuilder
                {
                    Name = "date",
                    Description = Lang.Get("th3essentials:slc-date")
                };
                _ = _client.Rest.CreateGuildCommand(date.Build(), _config.GuildId);

                SlashCommandBuilder restart = new SlashCommandBuilder
                {
                    Name = "restart",
                    Description = Lang.Get("th3essentials:slc-restart")
                };
                _ = _client.Rest.CreateGuildCommand(restart.Build(), _config.GuildId);

                List<SlashCommandOptionBuilder> channelOptions = new List<SlashCommandOptionBuilder>()
                {
                    new SlashCommandOptionBuilder()
                    {
                        Name = "channel",
                        Description = Lang.Get("th3essentials:slc-setchannel"),
                        Type = ApplicationCommandOptionType.Channel,
                        Required = true
                    }
                };
                SlashCommandBuilder setchannel = new SlashCommandBuilder
                {
                    Name = "setchannel",
                    Description = Lang.Get("th3essentials:slc-setchannel"),
                    Options = channelOptions
                };
                _ = _client.Rest.CreateGuildCommand(setchannel.Build(), _config.GuildId);

                List<SlashCommandOptionBuilder> whitelistOptions = new List<SlashCommandOptionBuilder>()
                {
                    new SlashCommandOptionBuilder()
                    {
                        Name = "playername",
                        Description = Lang.Get("th3essentials:slc-whitelist-playername"),
                        Type = ApplicationCommandOptionType.String,
                        Required = true
                    } ,
                    new SlashCommandOptionBuilder()
                    {
                        Name = "mode",
                        Description = Lang.Get("th3essentials:slc-whitelist-mode"),
                        Type = ApplicationCommandOptionType.Boolean,
                        Required = true
                    },
                    new SlashCommandOptionBuilder()
                    {
                        Name = "time",
                        Description = Lang.Get("th3essentials:slc-whitelist-time"),
                        Type = ApplicationCommandOptionType.Integer,
                    },
                    new SlashCommandOptionBuilder()
                    {
                        Name = "timetype",
                        Description = Lang.Get("th3essentials:slc-whitelist-timetype"),
                        Type = ApplicationCommandOptionType.String,
                        Choices = new List<ApplicationCommandOptionChoiceProperties>(){
                            new ApplicationCommandOptionChoiceProperties(){Name = "hours", Value = "hours"},
                            new ApplicationCommandOptionChoiceProperties(){Name = "days", Value = "days"},
                            new ApplicationCommandOptionChoiceProperties(){Name = "months", Value = "months"},
                            new ApplicationCommandOptionChoiceProperties(){Name = "years", Value = "years"}
                        }
                    },
                    new SlashCommandOptionBuilder()
                    {
                        Name = "reason",
                        Description = Lang.Get("th3essentials:slc-whitelist-reason"),
                        Type = ApplicationCommandOptionType.String
                    }
                };
                SlashCommandBuilder whitelist = new SlashCommandBuilder
                {
                    Name = "whitelist",
                    Description = Lang.Get("th3essentials:slc-whitelist"),
                    Options = whitelistOptions
                };
                _ = _client.Rest.CreateGuildCommand(whitelist.Build(), _config.GuildId);
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
                                    response = names.Count == 0 ? Lang.Get("th3essentials:slc-players-none") : string.Join("\n", names);
                                    break;
                                }
                            case "date":
                                {
                                    response = _api.World.Calendar.PrettyDate();
                                    break;
                                }
                            case "restart":
                                {
                                    if (_config.ShutdownTime != null)
                                    {
                                        TimeSpan restart = Th3Util.GetTimeTillRestart();
                                        response = Lang.Get("th3essentials:slc-restart-resp", restart.Hours.ToString("D2"), restart.Minutes.ToString("D2"));
                                    }
                                    else
                                    {
                                        response = Lang.Get("th3essentials:slc-restart-disabled");
                                    }
                                    break;
                                }
                            case "setchannel":
                                {
                                    if (commandInteraction.User is SocketGuildUser guildUser)
                                    {
                                        if (guildUser.GuildPermissions.Administrator)
                                        {
                                            SocketSlashCommandDataOption option = commandInteraction.Data.Options.First();
                                            if (option.Value is SocketTextChannel channel)
                                            {
                                                _config.ChannelId = channel.Id;
                                                if (!GetDiscordChannel())
                                                {
                                                    _api.Server.LogError($"Could not find channel with id: {_config.ChannelId}");
                                                    response = $"Could not find channel with id: {_config.ChannelId}";
                                                }
                                                else
                                                {
                                                    response = $"Channel was set to {channel.Name}";
                                                }
                                            }
                                            else
                                            {
                                                response = "Error: Channel needs to be a Text Channel";
                                            }
                                        }
                                        else
                                        {
                                            response = "You need to have Administrator permissions to do that";
                                        }
                                    }
                                    else
                                    {
                                        response = "Something went wrong: User was not a GuildUser";
                                    }
                                    break;
                                }
                            case "whitelist":
                                {
                                    if (commandInteraction.User is SocketGuildUser guildUser)
                                    {
                                        if (guildUser.GuildPermissions.Administrator)
                                        {
                                            string targetPlayer = null;
                                            bool? mode = null;
                                            long? time = null;
                                            string timetype = null;
                                            string reason = null;

                                            foreach (SocketSlashCommandDataOption option in commandInteraction.Data.Options)
                                            {
                                                switch (option.Name)
                                                {
                                                    case "playername":
                                                        {
                                                            targetPlayer = option.Value as string;
                                                            break;
                                                        }
                                                    case "mode":
                                                        {
                                                            mode = option.Value as bool?;
                                                            break;
                                                        }
                                                    case "time":
                                                        {
                                                            time = option.Value as long?;
                                                            break;
                                                        }
                                                    case "reason":
                                                        {
                                                            reason = option.Value as string;
                                                            break;
                                                        }
                                                    case "timetype": { timetype = option.Value as string; break; }
                                                    default:
                                                        {
                                                            _api.Logger.VerboseDebug("Something went wrong getting slc-whitelist option");
                                                            break;
                                                        }
                                                }
                                            }

                                            IServerPlayerData player = _api.PlayerData.GetPlayerDataByLastKnownName(targetPlayer);
                                            if (targetPlayer != null && mode != null && player != null)
                                            {
                                                if (mode == true)
                                                {
                                                    reason = reason ?? "";
                                                    timetype = timetype ?? "years";
                                                    int timenew = (int?)time ?? 50;
                                                    DateTime datetime = DateTime.Now.ToLocalTime();
                                                    switch (timetype)
                                                    {
                                                        case "hours":
                                                            {
                                                                datetime = datetime.AddHours(timenew);
                                                                break;
                                                            }
                                                        case "days":
                                                            {
                                                                datetime = datetime.AddDays(timenew);
                                                                break;
                                                            }
                                                        case "months":
                                                            {
                                                                datetime = datetime.AddMonths(timenew);
                                                                break;
                                                            }
                                                        case "years":
                                                        default:
                                                            {
                                                                datetime = datetime.AddYears(timenew);
                                                                break;
                                                            }
                                                    }
                                                    string name = guildUser.Nickname ?? guildUser.Username;
                                                    ((ServerMain)_api.World).PlayerDataManager.WhitelistPlayer(targetPlayer, player.PlayerUID, name, reason, datetime);
                                                    response = $"Player is now whitelisted until {datetime}";
                                                }
                                                else
                                                {
                                                    _ = ((ServerMain)_api.World).PlayerDataManager.UnWhitelistPlayer(targetPlayer, player.PlayerUID);
                                                    response = "Player is now removed from whitelist";
                                                }
                                            }
                                            else
                                            {
                                                response = "Could not find player";
                                            }
                                        }
                                        else
                                        {
                                            response = "You need to have Administrator permissions to do that";
                                        }
                                    }
                                    else
                                    {
                                        response = "Something went wrong: User was not a GuildUser";
                                    }
                                    break;
                                }
                            default:
                                {
                                    response = "Unknown SlashCommand";
                                    break;
                                }
                        }
                        _ = commandInteraction.RespondAsync(ServerMsg(response), ephemeral: _config.UseEphermalCmdResponse);
                        break;
                    }

                default:
                    break;
            }
            return Task.CompletedTask;
        }

        internal void SendServerMessage(string msg)
        {
            if (_discordChannel != null)
            {
                _ = _discordChannel.SendMessageAsync(ServerMsg(msg));
            }
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

        private bool GetDiscordChannel()
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
            UpdatePlayers(_api.World.AllOnlinePlayers.Length - 1);
            if (_discordChannel != null)
            {
                string msg = Lang.Get("th3essentials:disconnected", byPlayer.PlayerName);
                _ = _discordChannel.SendMessageAsync(ServerMsg(msg));
            }
        }

        private void PlayerNowPlayingAsync(IServerPlayer byPlayer)
        {
            UpdatePlayers();
            if (_discordChannel != null)
            {
                string msg = Lang.Get("th3essentials:connected", byPlayer.PlayerName);
                _ = _discordChannel.SendMessageAsync(ServerMsg(msg));
            }
        }

        private void UpdatePlayers(int players = -1)
        {
            if (players < 0)
            {
                players = _api.World.AllOnlinePlayers.Length;
            }
            _ = _client.SetGameAsync($"players: {players}");
        }

        private void GameReady()
        {
            if (_discordChannel != null)
            {
                _ = _discordChannel.SendMessageAsync(ServerMsg(Lang.Get("th3essentials:start")));
            }
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