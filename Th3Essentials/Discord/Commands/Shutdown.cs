using Discord;
using Discord.WebSocket;
using Vintagestory.API.Config;

namespace Th3Essentials.Discord.Commands;

public abstract class Shutdown
{
    public static SlashCommandProperties CreateCommand()
    {
        SlashCommandBuilder shutdown = new SlashCommandBuilder
        {
            Name = SlashCommands.Shutdown.ToString().ToLower(),
            Description = Lang.Get("th3essentials:slc-shutdown")
        };
        return shutdown.Build();
    }

    public static string HandleSlashCommand(Th3Discord discord, SocketSlashCommand commandInteraction, ref MessageComponent components)
    {
        if (commandInteraction.User is not SocketGuildUser guildUser)
            return "Something went wrong: User was not a GuildUser";
            
        if (!Th3SlashCommands.HasPermission(guildUser, discord.Config.ModerationRoles))
            return "You do not have permissions to do that";
                
        var builder = new ComponentBuilder().WithButton("Confirm", "shutdown-confirm");
        components = builder.Build();
        return "Do you really want to shutdown the server?";
    }
}