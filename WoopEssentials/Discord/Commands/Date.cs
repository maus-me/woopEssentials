using Discord;
using Vintagestory.API.Config;

namespace WoopEssentials.Discord.Commands;

public abstract class Date
{
    public static SlashCommandProperties CreateCommand()
    {
        var date = new SlashCommandBuilder
        {
            Name = SlashCommands.Date.ToString().ToLower(),
            Description = Lang.Get("woopessentials:slc-date")
        };
        return date.Build();
    }
}