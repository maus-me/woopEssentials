using Discord;
using Vintagestory.API.Config;

namespace Th3Essentials.Discord.Commands;

public abstract class Admins
{
    public static SlashCommandProperties CreateCommand()
    {
        var admins = new SlashCommandBuilder
        {
            Name = SlashCommands.Admins.ToString().ToLower(),
            Description = Lang.Get("woopessentials:slc-admins")
        };
        return admins.Build();
    }
}