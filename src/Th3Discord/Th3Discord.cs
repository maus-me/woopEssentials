using System;
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

        private Task LogAsync(LogMessage log)
        {
            _api.Server.LogVerboseDebug($"[Discord] {log.ToString()}");
            return Task.CompletedTask;
        }

        private Task ReadyAsync()
        {
            _api.Server.LogVerboseDebug("Discord ReadyAsync");
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

            _api.Server.LogVerboseDebug($"{_client.CurrentUser} is connected!");

            // get the channel to send messages from and to
            _discordChannel = _client.GetChannel(_config.ChannelId) as IMessageChannel;
            if (_discordChannel == null)
            {
                _api.Server.LogError($"Could not find channel with id: {_config.ChannelId}");
            }
            UpdatePlayers();
            return Task.CompletedTask;
        }

        private void CreateSlashCommand()
        {
            try
            {
                SlashCommandBuilder players = new SlashCommandBuilder
                {
                    Name = "players",
                    Description = "Get a list of Players"
                };
                // Now that we have our builder, we can call the rest API to make our slash command.
                _client.Rest.CreateGuildCommand(players.Build(), 319938033140891649);

                // With global commands we dont need the guild id.
                //_client.Rest.CreateGlobalCommand(players.Build());
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

                        if (commandInteraction.Data.Name == "players")
                        {
                            string response = "";
                            foreach (IServerPlayer player in _api.Server.Players)
                            {
                                response += player.PlayerName + " ";
                            }
                            commandInteraction.RespondAsync(response);
                        }
                        break;
                    }
            }
            return Task.CompletedTask;
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
                _discordChannel.SendMessageAsync("*" + msg + "*");
            }
        }

        private Task MessageReceivedAsync(SocketMessage message)
        {
            // The bot should never respond to itself.
            if (message.Author.Id == _client.CurrentUser.Id)
            {
                return Task.CompletedTask;
            }

            // only send messages from specific channel to vs server chat
            // ignore empty messages (message is empty when only picture/file is send)
            if (message.Channel.Id == _config.ChannelId && message.Content != "")
            {
                string msgRaw = CleanDiscordMessage(message.Content);
                if (msgRaw != "")
                {
                    string msg;
                    // use blue font ingame for discord messages
                    const string format = "<font color=\"#7289DA\"><strong>{0}:</strong></font> {1}";
                    msg = message.Attachments.Count > 0
                        ? string.Format(format, message.Author.Username, $" [Attachments] {msgRaw}")
                        : string.Format(format, message.Author.Username, msgRaw);
                    _api.SendMessageToGroup(GlobalConstants.GeneralChatGroup, msg, EnumChatType.OthersMessage);
                }
            }
            return Task.CompletedTask;
        }

        private string CleanDiscordMessage(string message)
        {
            message = Regex.Replace(message, "<(@|#)(&|!)*[0-9]*>", String.Empty);
            message = Regex.Replace(message, "(<)", "&lt;");
            message = Regex.Replace(message, "(>)", "&gt;");
            return message;
        }

        private void PlayerChatAsync(IServerPlayer byPlayer, int channelId, ref string message, ref string data, BoolRef consumed)
        {
            if (_discordChannel != null)
            {
                Match playerMsg = Regex.Match(message, "(^.*)(</strong>|</font>)(.*)");
                string msgRaw = playerMsg.Groups[3].Value;
                msgRaw = Regex.Replace(msgRaw, "&lt;", "<");
                msgRaw = Regex.Replace(msgRaw, "&gt;", ">");
                string msg = string.Format("**{0}:** {1}", byPlayer.PlayerName, msgRaw);
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
                _discordChannel.SendMessageAsync(msg);
            }
        }

        private void PlayerNowPlayingAsync(IServerPlayer byPlayer)
        {
            UpdatePlayers();
            if (_discordChannel != null)
            {
                string msg = Lang.Get("th3essentials:connected", byPlayer.PlayerName);
                _discordChannel.SendMessageAsync(msg);
            }
        }

        private void GameReady()
        {
            if (_discordChannel != null)
            {
                _discordChannel.SendMessageAsync(Lang.Get("th3essentials:start"));
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

        private async void Shutdown()
        {
            if (_client != null)
            {
                if (_discordChannel != null)
                {
                    await _discordChannel.SendMessageAsync(Lang.Get("th3essentials:shutdown"));
                }
                _client.Dispose();
            }
        }
    }
}