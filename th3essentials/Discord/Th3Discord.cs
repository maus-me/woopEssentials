using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using HarmonyLib;
using Th3Essentials.Config;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace Th3Essentials.Discord
{
    public class Th3Discord
    {
        private Harmony _harmony;

        private readonly string _harmonyPatchkey = "Th3Essentials.Discord.Patch";

        public static Th3Discord Instance { get; set; }

        private DiscordSocketClient _client;

        private IMessageChannel _discordChannel;

        internal ICoreServerAPI Sapi;

        internal Th3DiscordConfig Config;

        private bool _initialized;

        public static string[] TemporalStorm { get; set; }

        public static Dictionary<string, string> AccountsToLink { get; set; }

        public Dictionary<string, Rewards> rewards;

        public Th3Discord()
        {
            _initialized = false;
            TemporalStorm = new string[7]{
                Lang.Get("A light temporal storm is approaching"),
                Lang.Get("A light temporal storm is imminent"),
                Lang.Get("A medium temporal storm is approaching"),
                Lang.Get("A medium temporal storm is imminent"),
                Lang.Get("A heavy temporal storm is approaching"),
                Lang.Get("A heavy temporal storm is imminent"),
                Lang.Get("The temporal storm seems to be waning")
            };
            AccountsToLink = new Dictionary<string, string>();
            rewards = new Dictionary<string, Rewards>();
        }

        public void Init(ICoreServerAPI sapi)
        {
            Config = Th3Essentials.Config.DiscordConfig;
            Sapi = sapi;

            // create Discord client and set event methodes
            DiscordSocketConfig config = new DiscordSocketConfig()
            {
                GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildIntegrations | GatewayIntents.GuildMessages | GatewayIntents.GuildMembers,
                AlwaysDownloadUsers = true
            };
            _client = new DiscordSocketClient(config);

            _client.Ready += ReadyAsync;
            _client.Log += DiscordLog;

            Sapi.Server.LogVerboseDebug("Discord started");

            if (Sapi.World.Config.GetBool("temporalStability", true))
            {
                _harmony = new Harmony(_harmonyPatchkey);
                MethodInfo original = AccessTools.Method(typeof(SystemTemporalStability), "onTempStormTick");
                HarmonyMethod postfix = new HarmonyMethod(typeof(PatchSystemTemporalStability).GetMethod(nameof(PatchSystemTemporalStability.Postfix)));
                _harmony.Patch(original, postfix: postfix);
            }

            Sapi.RegisterCommand("dcauth", "Link ingame and discord account", string.Empty, OnDicordAuth, Privilege.chat);

            // start discord bot
            BotMainAsync();
            Instance = this;
        }

        private void OnDicordAuth(IServerPlayer player, int groupId, CmdArgs args)
        {
            string token = args.PopAll();
            foreach (KeyValuePair<string, string> account in AccountsToLink)
            {
                if (account.Key.Equals(token))
                {
                    if (Config.LinkedAccounts == null)
                    {
                        Config.LinkedAccounts = new Dictionary<string, string>();
                    }
                    Config.LinkedAccounts.Add(player.PlayerUID, account.Value);
                    player.SendMessage(GlobalConstants.GeneralChatGroup, "Discord - Vintagestory accounts linked", EnumChatType.CommandSuccess);
                    AccountsToLink.Remove(account.Key);
                    Th3Essentials.Config.MarkDirty();
                    return;
                }
            }
            player.SendMessage(GlobalConstants.GeneralChatGroup, "Could not find token", EnumChatType.CommandError);
        }

        private Task DiscordLog(LogMessage log)
        {
            switch (log.Severity)
            {
                case LogSeverity.Critical:
                    {
                        Sapi.Logger.Fatal($"[Discord] {log.ToString(prependTimestamp: false)}");
                        break;
                    }
                case LogSeverity.Error:
                    {
                        Sapi.Logger.Error($"[Discord] {log.ToString(prependTimestamp: false)}");
                        break;
                    }
                case LogSeverity.Warning:
                    {
                        Sapi.Logger.Warning($"[Discord] {log.ToString(prependTimestamp: false)}");
                        break;
                    }
                case LogSeverity.Info:
                case LogSeverity.Verbose:
                case LogSeverity.Debug:
                    {
                        Sapi.Logger.Debug($"[Discord] {log.ToString(prependTimestamp: false)}");
                        break;
                    }
            }
            return Task.CompletedTask;
        }

        public async void BotMainAsync()
        {
            await _client.LoginAsync(TokenType.Bot, Config.Token);
            await _client.StartAsync();
        }

        private Task ReadyAsync()
        {
            Sapi.Server.LogVerboseDebug($"{_client.CurrentUser.Username} is connected!");

            if (!GetDiscordChannel())
            {
                Sapi.Server.LogError($"Could not find channel with id: {Config.ChannelId}");
            }

            // needed since discord might disconect from the gateway and reconnect emitting the ReadyAsync again
            if (!_initialized)
            {
                _client.MessageReceived += MessageReceivedAsync;
                _client.InteractionCreated += InteractionCreated;
                _client.ButtonExecuted += ButtonExecuted;

                //add vs api events
                Sapi.Event.PlayerChat += PlayerChatAsync;
                Sapi.Event.PlayerDisconnect += PlayerDisconnectAsync;
                Sapi.Event.PlayerNowPlaying += PlayerNowPlayingAsync;
                Sapi.Event.ServerRunPhase(EnumServerRunPhase.GameReady, GameReady);
                Sapi.Event.ServerRunPhase(EnumServerRunPhase.Shutdown, Shutdown);

                if (Config.HelpRoleID != 0)
                {
                    _ = Sapi.RegisterCommand("requesthelp", Lang.Get("th3essentials:cd-help"), Lang.Get("th3essentials:cd-reply-param"), ReqestingHelp);
                }

                SocketGuild guild = _client.GetGuild(Config.GuildId);
                foreach (KeyValuePair<string, string> role in Th3Essentials.Config.DiscordConfig.RoleIdToName)
                {
                    SocketRole socketRole = guild.Roles.First((r) => r.Id == ulong.Parse(role.Key));
                    if (socketRole != null)
                    {
                        rewards.Add(role.Key, new Rewards(socketRole, role.Value));
                    }
                }

                _initialized = true;
            }

            _ = UpdatePlayers();
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
                Sapi.Logger.Error("Slashcommand create:" + exception.ToString());
                Sapi.Logger.Error("Maybe you forgot to add the applications.commands scope for your bot");
            }
        }

        private async void DeleteCommands()
        {
            try
            {
                IReadOnlyCollection<RestGuildCommand> commands = await _client.Rest.GetGuildApplicationCommands(Config.GuildId);
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
                Sapi.Logger.Error("Slashcommand delete:" + exception.ToString());
                Sapi.Logger.Error("Maybe you forgot to add the applications.commands scope for your bot");
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
            _ = _discordChannel?.SendMessageAsync(ServerMsg(msg));
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
                        Config.GuildId = guildUser.Guild.Id;
                        Config.ChannelId = message.Channel.Id;
                        Th3Essentials.Config.MarkDirty();

                        CreateSlashCommands();
                        if (!GetDiscordChannel())
                        {
                            Sapi.Server.LogError($"Could not find channel with id: {Config.ChannelId}");
                            _ = message.ReplyAsync($"Could not find channel with id: {Config.ChannelId}");
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
            else if (message.Channel.Id == Config.ChannelId && message.Content != "")
            {
                string msg = CleanDiscordMessage(message);
                // use blue font ingame for discord messages
                // const string format = "<font color=\"#{0}\"><strong>{1}: </strong></font><font family=\"Twitter Color Emoji\">{2}</font>";
                const string format = "<font color=\"#{0}\"><strong>{1}: </strong></font>{2}";
                if (message.Author is SocketGuildUser guildUser)
                {
                    string name = guildUser.Nickname ?? guildUser.Username;
                    msg = message.Attachments.Count > 0
                        ? string.Format(format, Config.DiscordChatColor, name, $" [Attachments] {msg}")
                        : string.Format(format, Config.DiscordChatColor, name, msg);
                    Sapi.SendMessageToGroup(GlobalConstants.GeneralChatGroup, msg, EnumChatType.OthersMessage);
                    Sapi.Logger.Chat(msg);
                }
            }
            return Task.CompletedTask;
        }

        internal bool GetDiscordChannel()
        {
            _discordChannel = _client.GetChannel(Config.ChannelId) as IMessageChannel;
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
                            RestGuildUser rgUser = _client.Rest.GetGuildUserAsync(Config.GuildId, mUser.Id).GetAwaiter().GetResult();
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

        public void ReqestingHelp(IServerPlayer player, int groupId, CmdArgs args)
        {
            SendServerMessage($"<@&{Config.HelpRoleID}> **{player.PlayerName}**: {args.PopAll()}");
            player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("th3essentials:cd-help-response"), EnumChatType.CommandSuccess);
        }

        private void PlayerChatAsync(IServerPlayer byPlayer, int channelId, ref string message, ref string data, BoolRef consumed)
        {
            if (_discordChannel != null && channelId == GlobalConstants.GeneralChatGroup)
            {
                Match playerMsg = Regex.Match(message, "> (.+)");
                string msg = playerMsg.Groups[1].Value;
                msg = msg.Replace("&lt;", "<").Replace("&gt;", ">");

                msg = Th3Essentials.Config.ShowRole && byPlayer.Role.PrivilegeLevel > 0
                    ? string.Format("**[{0}] {1}:** {2}", byPlayer.Role.Name, byPlayer.PlayerName, msg)
                    : string.Format("**{0}:** {1}", byPlayer.PlayerName, msg);
                _ = _discordChannel.SendMessageAsync(msg);
            }

            if (Th3Essentials.Config.ShowRole && byPlayer.Role.PrivilegeLevel > 0)
            {
                if (Th3Essentials.Config.DiscordConfig.Rewards)
                {
                    bool hasRewards = byPlayer.ServerData.CustomPlayerData.TryGetValue("th3rewards", out string id);
                    if (hasRewards && rewards.TryGetValue(id, out Rewards reward))
                    {
                        message = string.Format(Th3Essentials.Config.DiscordConfig.RoleFormatRewards, Th3Essentials.ToHex(byPlayer.Role.Color), byPlayer.Role.Name, Th3Essentials.ToHex(reward.SocketRole.Color), reward.Name, message);
                    }
                    else
                    {
                        message = string.Format(Th3Essentials.Config.RoleFormat, Th3Essentials.ToHex(byPlayer.Role.Color), byPlayer.Role.Name, message);
                    }
                }
                else
                {
                    message = string.Format(Th3Essentials.Config.RoleFormat, Th3Essentials.ToHex(byPlayer.Role.Color), byPlayer.Role.Name, message);
                }
            }
        }

        private void PlayerDisconnectAsync(IServerPlayer byPlayer)
        {
            // update player count with allplayer -1 since the disconnecting player is still online while this event fires
            int players = UpdatePlayers(-1);

            SendServerMessage(Lang.Get("th3essentials:disconnected", byPlayer.PlayerName, players, Sapi.Server.Config.MaxClients));
            if (Th3Essentials.Config.DiscordConfig.Rewards)
            {
                byPlayer.ServerData.CustomPlayerData.Remove("th3rewards");
            }
        }

        private void PlayerNowPlayingAsync(IServerPlayer byPlayer)
        {
            int players = UpdatePlayers(1);

            SendServerMessage(Lang.Get("th3essentials:connected", byPlayer.PlayerName, players, Sapi.Server.Config.MaxClients));

            if (Th3Essentials.Config.DiscordConfig.Rewards)
            {
                byPlayer.ServerData.CustomPlayerData.Remove("th3rewards");
                if (Config.LinkedAccounts.TryGetValue(byPlayer.PlayerUID, out string discordid))
                {
                    SocketGuild guild = _client.GetGuild(Config.GuildId);
                    SocketGuildUser socketGuildUser = guild.GetUser(ulong.Parse(discordid));

                    foreach (KeyValuePair<string, string> role in Th3Essentials.Config.DiscordConfig.RoleIdToName)
                    {
                        SocketRole socketRole = socketGuildUser.Roles.First((r) => r.Id.ToString() == role.Key);
                        if (socketRole != null)
                        {
                            Sapi.Logger.VerboseDebug($"{Th3Essentials.ToHex(socketRole.Color)}, {socketRole.Name}, {socketRole.Id}");
                            byPlayer.ServerData.CustomPlayerData["th3rewards"] = socketRole.Id.ToString();
                        }
                    }
                }
            }
        }

        private int UpdatePlayers(int players = 0)
        {
            players += Sapi.Server.Players.Count(pl => pl.ConnectionState == EnumClientState.Playing);
            players = Math.Max(0, players);
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
                await _client.StopAsync();
                await _client.LogoutAsync();
            }
        }

        public void Dispose()
        {
            _client.Ready -= ReadyAsync;

            if (_initialized)
            {
                _initialized = false;
                _client.MessageReceived -= MessageReceivedAsync;
                _client.InteractionCreated -= InteractionCreated;
                _client.ButtonExecuted -= ButtonExecuted;

                Sapi.Event.PlayerChat -= PlayerChatAsync;
                Sapi.Event.PlayerDisconnect -= PlayerDisconnectAsync;
                Sapi.Event.PlayerNowPlaying -= PlayerNowPlayingAsync;
            }

            _harmony?.UnpatchAll(_harmonyPatchkey);
        }

        internal string ServerMsg(string msg)
        {
            return $"*{msg}*";
        }

        public class PatchSystemTemporalStability
        {
            private static TemporalStormRunTimeData data;
            private static ICoreAPI api;
            private static int StormState = 0;

            private static void GetFields(SystemTemporalStability __instance)
            {
                if (data == null)
                {
                    data = (TemporalStormRunTimeData)AccessTools.Field(typeof(SystemTemporalStability), "data").GetValue(__instance);
                }
                if (api == null)
                {
                    api = (ICoreAPI)AccessTools.Field(typeof(SystemTemporalStability), "api").GetValue(__instance);
                }
            }

            public static void Postfix(SystemTemporalStability __instance)
            {
                GetFields(__instance);
                double nextStormDaysLeft = data.nextStormTotalDays - api.World.Calendar.TotalDays;
                double activeDaysLeft = data.stormActiveTotalDays - api.World.Calendar.TotalDays;

                // Approaching
                if (nextStormDaysLeft > 0.03 && nextStormDaysLeft < 0.35 && StormState != 1)
                {
                    StormState = 1;
                    int i = 0;
                    switch (data.nextStormStrength)
                    {
                        case EnumTempStormStrength.Light:
                            i = 0;
                            break;
                        case EnumTempStormStrength.Medium:
                            i = 2;
                            break;
                        case EnumTempStormStrength.Heavy:
                            i = 4;
                            break;
                        default:
                            break;
                    }
                    Instance.SendServerMessage(Lang.Get("th3essentials:temporalStormPrefix") + TemporalStorm[i]);
                }
                // Imminent
                else if (nextStormDaysLeft > 0 && nextStormDaysLeft <= 0.02 && StormState != 2)
                {
                    StormState = 2;
                    int i = 0;
                    switch (data.nextStormStrength)
                    {
                        case EnumTempStormStrength.Light:
                            i = 1;
                            break;
                        case EnumTempStormStrength.Medium:
                            i = 3;
                            break;
                        case EnumTempStormStrength.Heavy:
                            i = 5;
                            break;
                        default:
                            break;
                    }
                    Instance.SendServerMessage(Lang.Get("th3essentials:temporalStormPrefix") + TemporalStorm[i]);
                }
                // Waning
                else if (nextStormDaysLeft < 0 && activeDaysLeft > 0 && activeDaysLeft < 0.02 && StormState != 3)
                {
                    StormState = 3;
                    Instance.SendServerMessage(Lang.Get("th3essentials:temporalStormPrefix") + TemporalStorm[6]);
                }
            }
        }
    }
}