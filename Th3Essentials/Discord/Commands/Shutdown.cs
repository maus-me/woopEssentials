using Discord;
using Discord.WebSocket;
using Vintagestory.API.Config;

namespace Th3Essentials.Discord.Commands;

public abstract class Shutdown
{
    public static SlashCommandProperties CreateCommand()
    {
        var shutdown = new SlashCommandBuilder
        {
            Name = SlashCommands.Shutdown.ToString().ToLower(),
            Description = Lang.Get("woopessentials:slc-shutdown")
        };
        return shutdown.Build();
    }

    public static string HandleSlashCommand(WoopDiscord discord, SocketSlashCommand commandInteraction, ref MessageComponent? components)
    {
        if (commandInteraction.User is not SocketGuildUser guildUser)
            return "Something went wrong: User was not a GuildUser";
            
        if (!WoopSlashCommands.HasPermission(guildUser, discord.Config.ModerationRoles))
            return "You do not have permissions to do that";
                
        var builder = new ComponentBuilder().WithButton("Confirm", "shutdown-confirm");
        components = builder.Build();
        return "Do you really want to shutdown the server?";
    }
}