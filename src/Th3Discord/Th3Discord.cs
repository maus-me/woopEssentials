using System;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;
using Th3Essentials.Config;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;

namespace Th3Essentials.Discord
{
    public class Th3Discord
    {
        private DiscordClient _client;

        private ICoreServerAPI _api;

        private DiscordChannel _discordChannel;

        private Th3Config _config;

        private int PlayersOnline
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get;
            [MethodImpl(MethodImplOptions.Synchronized)]
            set;
        }

        private bool initialized;

        public Th3Discord()
        {
            PlayersOnline = 0;
            initialized = false;
        }

        public void Init(ICoreServerAPI api)
        {
            _config = Th3Essentials.Config;
            _api = api;


            // create Discord client and set event methodes
            _client = new DiscordClient(new DiscordConfiguration()
            {
                Token = _config.Token,
                TokenType = TokenType.Bot,
                LoggerFactory = new Th3LoggerFactory(_api, LogLevel.Debug)
            });

            _client.Ready += ReadyAsync;

            // start discord bot
            BotMainAsync();
        }

        public async void BotMainAsync()
        {
            await _client.ConnectAsync();

            // keep the discord bot thread running
            await Task.Delay(Timeout.Infinite);
        }

        private Task ReadyAsync(DiscordClient sender, ReadyEventArgs e)
        {
            // needed since discord might disconect from the gateway and reconnect emitting the ReadyAsync again
            if (!initialized)
            {
                _client.MessageCreated += MessageReceivedAsync;

                //add vs api events
                _api.Event.PlayerChat += PlayerChatAsync;
                _api.Event.PlayerDisconnect += PlayerDisconnectAsync;
                _api.Event.PlayerNowPlaying += PlayerNowPlayingAsync;
                _api.Event.GameWorldSave += WorldSaveCreated;
                _api.Event.PlayerDeath += PlayerDeathAsync;
                _api.Event.ServerRunPhase(EnumServerRunPhase.GameReady, GameReady);
                _client.CreateGuildApplicationCommandAsync(319938033140891649, new DiscordApplicationCommand("players", "Get a list of Players"));
                _client.InteractionCreated += InteractionCreated;

                initialized = true;
            }

            _api.Server.LogVerboseDebug($"{_client.CurrentUser} is connected!");

            // get the channel to send messages from and to
            _client.GetChannelAsync(_config.ChannelId).ContinueWith(task =>
            {
                _discordChannel = task.Result;
                if (_discordChannel == null)
                {
                    _api.Server.LogError($"Could not find channel with id: {_config.ChannelId}");
                }
            });
            UpdatePlayers();
            return Task.CompletedTask;
        }

        private Task InteractionCreated(DiscordClient sender, InteractionCreateEventArgs e)
        {
            if (e.Interaction.Data.Name == "players")
            {
                string response = "";
                foreach (IServerPlayer player in _api.Server.Players)
                {
                    response += player.PlayerName + " ";
                }
                DiscordInteractionResponseBuilder resp = new DiscordInteractionResponseBuilder
                {
                    Content = response
                };
                e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, resp);
                e.Handled = true;
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

        private Task MessageReceivedAsync(DiscordClient sender, MessageCreateEventArgs e)
        {
            // The bot should never respond to itself.
            if (e.Author.Id == _client.CurrentUser.Id)
            {
                return Task.CompletedTask;
            }

            // only send messages from specific channel to vs server chat
            // ignore empty messages (message is empty when only picture/file is send)
            if (e.Channel.Id == _config.ChannelId && e.Message.Content != "")
            {
                string msgRaw = CleanDiscordMessage(e.Message.Content);
                if (msgRaw != "")
                {
                    string msg;
                    // use blue font ingame for discord messages
                    const string format = "<font color=\"#7289DA\"><strong>{0}:</strong></font> {1}";
                    msg = e.Message.Attachments.Count > 0
                        ? string.Format(format, e.Author.Username, $" [Attachments] {msgRaw}")
                        : string.Format(format, e.Author.Username, msgRaw);
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
            PlayersOnline--;
            UpdatePlayers();
            if (_discordChannel != null)
            {
                string msg = Lang.Get("th3essentials:disconnected", byPlayer.PlayerName);
                _discordChannel.SendMessageAsync(msg);
            }
        }

        private void PlayerNowPlayingAsync(IServerPlayer byPlayer)
        {
            PlayersOnline++;
            UpdatePlayers();
            if (_discordChannel != null)
            {
                string msg = Lang.Get("th3essentials:connected", byPlayer.PlayerName);
                _discordChannel.SendMessageAsync(msg);
            }
        }

        private void WorldSaveCreated()
        {
            if (PlayersOnline != _api.World.AllOnlinePlayers.Length)
            {
                _api.Server.LogVerboseDebug($"Player number incorrect {PlayersOnline} / {_api.World.AllOnlinePlayers.Length}");
            }
            PlayersOnline = _api.World.AllOnlinePlayers.Length;
            UpdatePlayers();
        }

        private void GameReady()
        {
            if (_discordChannel != null)
            {
                _discordChannel.SendMessageAsync(Lang.Get("th3essentials:start"));
            }
        }

        private void UpdatePlayers()
        {
            _client.UpdateStatusAsync(new DiscordActivity($"players: {PlayersOnline}"));
        }

        public void Dispose()
        {
            if (_client != null)
            {
                ShutdownDiscord();
            }
        }

        private async void ShutdownDiscord()
        {
            if (_discordChannel != null)
            {
                await _discordChannel.SendMessageAsync(Lang.Get("th3essentials:shutdown"));
            }
            _client.Dispose();
        }
    }
}