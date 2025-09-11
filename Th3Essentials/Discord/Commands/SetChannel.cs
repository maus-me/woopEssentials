using System.Collections.Generic;
using System.Linq;
using Discord;
using Discord.WebSocket;
using Vintagestory.API.Config;

namespace Th3Essentials.Discord.Commands;

public abstract class SetChannel
{
    public static SlashCommandProperties CreateCommand()
    {
        var channelOptions = new List<SlashCommandOptionBuilder>()
        {
            new()
            {
                Name = "channel",
                Description = Lang.Get("woopessentials:slc-setchannel"),
                Type = ApplicationCommandOptionType.Channel,
                IsRequired = true
            }
        };
        var setchannel = new SlashCommandBuilder
        {
            Name = SlashCommands.SetChannel.ToString().ToLower(),
            Description = Lang.Get("woopessentials:slc-setchannel"),
            Options = channelOptions
        };
        return setchannel.Build();
    }

    public static string HandleSlashCommand(WoopDiscord discord, SocketSlashCommand commandInteraction)
    {
        if (commandInteraction.User is not SocketGuildUser guildUser)
            return "Something went wrong: User was not a GuildUser";

        if (!guildUser.GuildPermissions.Administrator)
            return "You need to have Administrator permissions to do that";
        
        var option = commandInteraction.Data.Options.First();
        if (option.Value is not SocketTextChannel channel)
            return "Error: Channel needs to be a Text Channel";
        
        discord.Config.ChannelId = channel.Id;
        WoopEssentials.Config.MarkDirty();
        if (discord.GetDiscordChannel())
            return $"Channel was set to {channel.Name}";
        
        discord.Sapi.Server.LogError($"Could not find channel with id: {discord.Config.ChannelId}");
        return $"Could not find channel with id: {discord.Config.ChannelId}";

    }
}