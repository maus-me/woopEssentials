using Discord;
using Vintagestory.API.Config;

namespace Th3Essentials.Discord.Commands;

public abstract class Date
{
    public static SlashCommandProperties CreateCommand()
    {
        SlashCommandBuilder date = new SlashCommandBuilder
        {
            Name = SlashCommands.Date.ToString().ToLower(),
            Description = Lang.Get("th3essentials:slc-date")
        };
        return date.Build();
    }
}