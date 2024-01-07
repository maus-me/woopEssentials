using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using HarmonyLib;
using Th3Essentials.Config;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
// ReSharper disable InconsistentNaming

namespace Th3Essentials.Discord;

public class Th3Discord
{
    private const string REWARDS_SERVER_DATA_KEY = "th3rewards";
    private Harmony? _harmony;

    private readonly string _harmonyPatchkey = "Th3Essentials.Discord.Patch";

    public static Th3Discord Instance { get; private set; } = null!;

    private DiscordSocketClient _client = null!;

    private IMessageChannel? _discordChannel;
    private IMessageChannel? _adminLogChannel;

    internal ICoreServerAPI Sapi = null!;

    private Th3Essentials _th3Essentials = null!;

    internal Th3DiscordConfig Config = null!;

    private bool _initialized;

    public static string[] TemporalStorm { get; } = {
        Lang.Get("A light temporal storm is approaching"),
        Lang.Get("A light temporal storm is imminent"),
        Lang.Get("A medium temporal storm is approaching"),
        Lang.Get("A medium temporal storm is imminent"),
        Lang.Get("A heavy temporal storm is approaching"),
        Lang.Get("A heavy temporal storm is imminent"),
        Lang.Get("The temporal storm seems to be waning")
    };

    public static Dictionary<string, string> AccountsToLink { get; } = new();

    public readonly Dictionary<string, Rewards> Rewards;

    public Th3Discord()
    {
        Instance = this;
        _initialized = false;
        Rewards = new Dictionary<string, Rewards>();
    }

    public void Init(Th3Essentials th3Essentials)
    {
        Config = Th3Essentials.Config.DiscordConfig!;
        Sapi = th3Essentials.Sapi;
        _th3Essentials = th3Essentials;

        // create Discord client and set event methodes
        var config = new DiscordSocketConfig()
        {
            GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildIntegrations | GatewayIntents.GuildMessages | GatewayIntents.MessageContent
        };

        if (Config.Rewards)
        {
            config.GatewayIntents |= GatewayIntents.GuildMembers;
            config.AlwaysDownloadUsers = true;
        }

        _client = new DiscordSocketClient(config);

        _client.Ready += ReadyAsync;
        _client.Log += DiscordLog;

        Sapi.Server.LogVerboseDebug("Discord started");

        if (Sapi.World.Config.GetBool("temporalStability", true))
        {
            _harmony ??= new Harmony(_harmonyPatchkey);
            var original = AccessTools.Method(typeof(SystemTemporalStability), "onTempStormTick");
            var postfix = new HarmonyMethod(typeof(PatchSystemTemporalStability).GetMethod(nameof(PatchSystemTemporalStability.Postfix)));
            _harmony.Patch(original, postfix: postfix);
        }

        if (Config.AdminLogChannelId != 0)
        {
            _harmony ??= new Harmony(_harmonyPatchkey);
            PatchAdminLogging.Patch(_harmony);
        }
            
        Sapi.ChatCommands.Create("dcauth")
            .WithDescription("Link ingame and discord account")
            .RequiresPlayer()
            .RequiresPrivilege(Privilege.chat)
            .WithArgs(Sapi.ChatCommands.Parsers.Word("token"))
            .HandleWith(OnDiscordAuth);

        // start discord bot
        BotMainAsync();
    }

    private TextCommandResult OnDiscordAuth(TextCommandCallingArgs args)
    {
        var token = args.Parsers[0].GetValue() as string;
        var player = args.Caller.Player;
        foreach (var account in AccountsToLink.Where(account => account.Key.Equals(token)))
        {
            Config.LinkedAccounts ??= new Dictionary<string, string>();
                    
            Config.LinkedAccounts.Add(player.PlayerUID, account.Value);
            AccountsToLink.Remove(account.Key);
            Th3Essentials.Config.MarkDirty();
            return TextCommandResult.Success("Discord - Vintagestory accounts linked");
        }
        return TextCommandResult.Error("Could not find token");
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
                var logMessage = log.ToString(prependTimestamp: false);
                if (log.Exception is GatewayReconnectException || log.Exception?.InnerException is WebSocketException)
                {
                    Sapi.Logger.VerboseDebug($"[Discord] {logMessage}");
                }
                else
                {
                    Sapi.Logger.Warning($"[Discord] {logMessage}");
                }
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
        try
        {
            Sapi.Server.LogVerboseDebug($"{_client.CurrentUser.GlobalName} is connected!");

            if (!GetDiscordChannel())
            {
                Sapi.Server.LogError($"Could not find channel with id: {Config.ChannelId}");
            }

            // needed since discord might disconect from the gateway and reconnect emitting the ReadyAsync again
            if (!_initialized)
            {
                _client.MessageReceived += MessageReceivedAsync;
                _client.SlashCommandExecuted += InteractionCreated;
                _client.ButtonExecuted += ButtonExecuted;

                //add vs api events
                Sapi.Event.PlayerChat += PlayerChatAsync;
                Sapi.Event.PlayerDisconnect += PlayerDisconnectAsync;
                Sapi.Event.PlayerNowPlaying += PlayerNowPlayingAsync;
                Sapi.Event.ServerRunPhase(EnumServerRunPhase.Shutdown, Shutdown);
                    
                SendServerMessage(Lang.Get("th3essentials:start"));

                if (Config.HelpRoleID != 0)
                {
                    Sapi.ChatCommands.Create("requesthelp")
                        .WithDescription(Lang.Get("th3essentials:cd-help"))
                        .RequiresPlayer()
                        .RequiresPrivilege(Privilege.chat)
                        .WithArgs(Sapi.ChatCommands.Parsers.All("message"))
                        .HandleWith(RequestingHelp);
                }

                if (Config.Rewards && Config.RewardIdToName != null)
                {
                    var guild = _client.GetGuild(Config.GuildId);
                    foreach (var role in Config.RewardIdToName)
                    {
                        var socketRole = guild.Roles.First((r) => r.Id == ulong.Parse(role.Key));
                        Rewards.Add(role.Key, new Rewards(socketRole, role.Value));
                    }
                }

                if (Config.AutoAddSlashCommands)
                {
                    Task.Run(async () =>
                    {
                        var guild = _client.GetGuild(Config.GuildId);
                        var applicationCommandsAsync = await guild.GetApplicationCommandsAsync();
                        var commands = Enum.GetNames(typeof(SlashCommands)).Select(x => x.ToLower());
                        var discordCommands = applicationCommandsAsync.Select(x => x.Name.ToLower()).ToList();
                        if (commands.Any(command => !discordCommands.Contains(command)))
                        {
                            CreateSlashCommands();
                        }
                    });
                }

                _initialized = true;
            }

            _ = UpdatePlayers();
        }
        catch (Exception e)
        {
            Sapi.Server.LogError($"Exception: {e.Message}");
        }
        return Task.CompletedTask;
    }

    private void CreateSlashCommands()
    {
        try
        {
            Th3SlashCommands.CreateGuildCommands(_client, Sapi);
        }
        catch (Exception exception)
        {
            Sapi.Logger.Error("Slashcommand create:" + exception);
            Sapi.Logger.Error("Maybe you forgot to add the applications.commands scope for your bot");
        }
    }

    private Task InteractionCreated(SocketSlashCommand command)
    {
        Th3SlashCommands.HandleSlashCommand(this, command);
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

    internal void SendAdminLog(string msg)
    {
        _ = _adminLogChannel?.SendMessageAsync(msg);
    }

    private Task MessageReceivedAsync(SocketMessage messageParam)
    {
        if (messageParam.Author.Id == _client.CurrentUser.Id)
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
            if (message.Author is SocketGuildUser { GuildPermissions.Administrator: true } guildUser)
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
        // only send messages from specific channel to vs server chat
        // ignore empty messages (message is empty when only picture/file is send)
        else if (Config.DiscordChatRelay && message.Channel.Id == Config.ChannelId && message.Content != "")
        {
            var msg = CleanDiscordMessage(message);
            // use blue font ingame for discord messages
            // const string format = "<font color=\"#{0}\"><strong>{1}: </strong></font><font family=\"Twitter Color Emoji\">{2}</font>";
            const string format = "<font color=\"#{0}\"><strong>{1}: </strong></font>{2}";
            string name;

            if (message.Author is SocketGuildUser guildUser)
            {
                name = guildUser.DisplayName;
            }
            else
            {
                name = message.Author.GlobalName ?? message.Author.Username;
            }
            msg = message.Attachments.Count > 0
                ? string.Format(format, Config.DiscordChatColor, name, $" [Attachments] {msg}")
                : string.Format(format, Config.DiscordChatColor, name, msg);
            Sapi.SendMessageToGroup(GlobalConstants.GeneralChatGroup, msg, EnumChatType.OthersMessage);
            Sapi.Logger.Chat(msg);
        }
        return Task.CompletedTask;
    }

    internal bool GetDiscordChannel()
    {
        _discordChannel = _client.GetChannel(Config.ChannelId) as IMessageChannel;
        if (Config.AdminLogChannelId != 0)
        {
            _adminLogChannel = _client.GetChannel(Config.AdminLogChannelId) as IMessageChannel;
        }
        return _discordChannel != null;
    }

    private string CleanDiscordMessage(SocketMessage message)
    {
        var msg = message.Content;
        // find user id (https://discord.com/developers/docs/reference#message-formatting)
        var userMention = Regex.Matches(msg, "<@!?([0-9]+)>");
        foreach (Match user in userMention)
        {
            foreach (var mUser in message.MentionedUsers)
            {
                if (mUser.Id.ToString() == user.Groups[1].Value)
                {
                    var name = "Unknown";
                    if (mUser is SocketGuildUser mGuildUser)
                    {
                        name = mGuildUser.DisplayName;
                    }
                    else if (mUser is SocketUnknownUser)
                    {
                        var rgUser = _client.Rest.GetGuildUserAsync(Config.GuildId, mUser.Id).GetAwaiter().GetResult();
                        name = rgUser.DisplayName;
                    }
                    msg = Regex.Replace(msg, $"<@!?{user.Groups[1].Value}>", $"@{name}");
                    break;
                }
            }
        }

        var roleMention = Regex.Matches(msg, "<@&([0-9]+)>");
        foreach (Match role in roleMention)
        {
            foreach (var mRole in message.MentionedRoles)
            {
                if (mRole.Id.ToString() == role.Groups[1].Value)
                {
                    msg = msg.Replace($"<@&{role.Groups[1].Value}>", $"@{mRole.Name}");
                    break;
                }
            }
        }

        var channelMention = Regex.Matches(msg, "<#([0-9]+)>");
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

    private TextCommandResult RequestingHelp(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player;
        var msg = args.Parsers[0].GetValue() as string;
        SendServerMessage($"<@&{Config.HelpRoleID}> **{player.PlayerName}**: {msg}");
        return TextCommandResult.Success(Lang.Get("th3essentials:cd-help-response"));
    }

    private void PlayerChatAsync(IServerPlayer byPlayer, int channelId, ref string message, ref string data, BoolRef consumed)
    {
        if (Config.DiscordChatRelay && _discordChannel != null && channelId == GlobalConstants.GeneralChatGroup)
        {
            var playerMsg = Regex.Match(message, "> (.+)");
            var msg = playerMsg.Groups[1].Value;
            msg = msg.Replace("&lt;", "<").Replace("&gt;", ">").Replace("@here", "@_here").Replace("@everyone", "@_everyone");

            if (Th3Essentials.Config.ShowRole && (Th3Essentials.Config.ShowRoles == null || Th3Essentials.Config.ShowRoles.Contains(byPlayer.Role.Code)))
            {
                if (Config.Rewards && byPlayer.ServerData.CustomPlayerData.TryGetValue(REWARDS_SERVER_DATA_KEY, out var id) && Rewards.TryGetValue(id, out var reward))
                {
                    msg = $"**[{byPlayer.Role.Name}] [{reward.Name}] {byPlayer.PlayerName}:** {msg}";
                }
                else
                {
                    msg = $"**[{byPlayer.Role.Name}] {byPlayer.PlayerName}:** {msg}";
                }
            }
            else
            {
                if (Config.Rewards && byPlayer.ServerData.CustomPlayerData.TryGetValue(REWARDS_SERVER_DATA_KEY, out var id) && Rewards.TryGetValue(id, out var reward))
                {
                    msg = $"**[{reward.Name}] {byPlayer.PlayerName}:** {msg}";
                }
                else
                {
                    msg = $"**{byPlayer.PlayerName}:** {msg}";
                }
            }
            _ = _discordChannel.SendMessageAsync(msg);
        }

        if (Th3Essentials.Config.ShowRole && (Th3Essentials.Config.ShowRoles == null || Th3Essentials.Config.ShowRoles.Contains(byPlayer.Role.Code)))
        {
            if (Config.Rewards && byPlayer.ServerData.CustomPlayerData.TryGetValue(REWARDS_SERVER_DATA_KEY, out var id) && Rewards.TryGetValue(id, out var reward))
            {
                message = string.Format(Config.RoleRewardsFormat, Th3Essentials.ToHex(byPlayer.Role.Color), byPlayer.Role.Name, Th3Essentials.ToHex(reward.SocketRole.Color), reward.Name, message);
            }
            else
            {
                message = string.Format(Th3Essentials.Config.RoleFormat, Th3Essentials.ToHex(byPlayer.Role.Color), byPlayer.Role.Name, message);
            }
        }
        else if (Config.Rewards)
        {
            if (byPlayer.ServerData.CustomPlayerData.TryGetValue(REWARDS_SERVER_DATA_KEY, out var id) && Rewards.TryGetValue(id, out var reward))
            {
                message = string.Format(Config.RewardsFormat, Th3Essentials.ToHex(reward.SocketRole.Color), reward.Name, message);
            }
        }

        if (!string.IsNullOrEmpty(Th3Essentials.Config.ChatTimestampFormat))
        {
            var now = DateTime.Now;
            message = $"{now.TimeOfDay.ToString(Th3Essentials.Config.ChatTimestampFormat)}: {message}";
        }
    }

    private void PlayerDisconnectAsync(IServerPlayer byPlayer)
    {
        // update player count with allplayer -1 since the disconnecting player is still online while this event fires
        var players = UpdatePlayers(-1);

        SendServerMessage(Lang.Get("th3essentials:disconnected", byPlayer.PlayerName, players, Sapi.Server.Config.MaxClients));
        if (Config.Rewards)
        {
            byPlayer.ServerData.CustomPlayerData.Remove(REWARDS_SERVER_DATA_KEY);
        }
    }

    private void PlayerNowPlayingAsync(IServerPlayer byPlayer)
    {
        var players = UpdatePlayers(1);

        SendServerMessage(Lang.Get("th3essentials:connected", byPlayer.PlayerName, players, Sapi.Server.Config.MaxClients));

        if (!Config.Rewards || Config.LinkedAccounts == null || Config.RewardIdToName == null) return;
            
        byPlayer.ServerData.CustomPlayerData.Remove(REWARDS_SERVER_DATA_KEY);
        if (!Config.LinkedAccounts.TryGetValue(byPlayer.PlayerUID, out var discordId)) return;
            
        var guild = _client.GetGuild(Config.GuildId);
        var socketGuildUser = guild.GetUser(ulong.Parse(discordId));

        foreach (KeyValuePair<string, string> role in Config.RewardIdToName)
        {
            var socketRole = socketGuildUser.Roles.FirstOrDefault((r) => r.Id.ToString() == role.Key);
            if (socketRole != null)
            {
                // Sapi.Logger.VerboseDebug($"{Th3Essentials.ToHex(socketRole.Color)}, {socketRole.Name}, {socketRole.Id}");
                var discordRewardId = socketRole.Id.ToString();
                byPlayer.ServerData.CustomPlayerData[REWARDS_SERVER_DATA_KEY] = discordRewardId;
                _th3Essentials.PlayerWithRewardJoin(byPlayer, discordRewardId);
                break;
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

    private async void Shutdown()
    {
        if (_discordChannel != null)
        {
            _ = await _discordChannel.SendMessageAsync(ServerMsg(Lang.Get("th3essentials:shutdown")));
        }
        await _client.StopAsync();
        await _client.LogoutAsync();
    }

    public void Dispose()
    {
        _client.Ready -= ReadyAsync;

        if (_initialized)
        {
            _initialized = false;
            _client.MessageReceived -= MessageReceivedAsync;
            _client.SlashCommandExecuted -= InteractionCreated;
            _client.ButtonExecuted -= ButtonExecuted;

            Sapi.Event.PlayerChat -= PlayerChatAsync;
            Sapi.Event.PlayerDisconnect -= PlayerDisconnectAsync;
            Sapi.Event.PlayerNowPlaying -= PlayerNowPlayingAsync;
        }

        _harmony?.UnpatchAll(_harmonyPatchkey);
    }

    internal string ServerMsg(string msg)
    {
        return $"***{msg}***";
    }

    public class PatchSystemTemporalStability
    {
        private static TemporalStormRunTimeData? _data;
        private static ICoreAPI? _api;
        private static int _stormState;

        private static void GetFields(SystemTemporalStability __instance)
        {
            _data ??= (TemporalStormRunTimeData)AccessTools.Field(typeof(SystemTemporalStability), "data").GetValue(__instance)!;
            _api ??= (ICoreAPI)AccessTools.Field(typeof(SystemTemporalStability), "api").GetValue(__instance)!;
        }

        public static void Postfix(SystemTemporalStability __instance)
        {
            GetFields(__instance);
            var nextStormDaysLeft = _data!.nextStormTotalDays - _api!.World.Calendar.TotalDays;
            var activeDaysLeft = _data.stormActiveTotalDays - _api.World.Calendar.TotalDays;

            // Approaching
            if (nextStormDaysLeft > 0.03 && nextStormDaysLeft < 0.35 && _stormState != 1)
            {
                _stormState = 1;
                var i = _data.nextStormStrength switch
                {
                    EnumTempStormStrength.Light => 0,
                    EnumTempStormStrength.Medium => 2,
                    EnumTempStormStrength.Heavy => 4,
                    _ => 0
                };
                Instance.SendServerMessage(Lang.Get("th3essentials:temporalStormPrefix") + TemporalStorm[i]);
            }
            // Imminent
            else if (nextStormDaysLeft > 0 && nextStormDaysLeft <= 0.02 && _stormState != 2)
            {
                _stormState = 2;
                var i = _data.nextStormStrength switch
                {
                    EnumTempStormStrength.Light => 1,
                    EnumTempStormStrength.Medium => 3,
                    EnumTempStormStrength.Heavy => 5,
                    _ => 0
                };
                Instance.SendServerMessage(Lang.Get("th3essentials:temporalStormPrefix") + TemporalStorm[i]);
            }
            // Waning
            else if (nextStormDaysLeft < 0 && activeDaysLeft > 0 && activeDaysLeft < 0.02 && _stormState != 3)
            {
                _stormState = 3;
                Instance.SendServerMessage(Lang.Get("th3essentials:temporalStormPrefix") + TemporalStorm[6]);
            }
        }
    }
}