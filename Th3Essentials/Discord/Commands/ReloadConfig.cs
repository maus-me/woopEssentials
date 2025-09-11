using System;
using Discord;
using Discord.WebSocket;
using Th3Essentials.Config;
using Vintagestory.API.Config;

namespace Th3Essentials.Discord.Commands;

public abstract class ReloadConfig
{
    public static SlashCommandProperties CreateCommand()
    {
        var reloadConfig = new SlashCommandBuilder
        {
            Name = SlashCommands.ReloadConfig.ToString().ToLower(),
            Description = Lang.Get("woopessentials:slc-reload")
        };
        return reloadConfig.Build();
    }

    public static string HandleSlashCommand(WoopDiscord discord, SocketSlashCommand commandInteraction)
    {
        if (!(commandInteraction.User is SocketGuildUser guildUser))
        {
            return "You do not have permissions to do that";
        }

        if (!WoopSlashCommands.HasPermission(guildUser, discord.Config.ModerationRoles))
        {
            return "You do not have permissions to do that";
        }

        try
        {
            var configTemp = discord.Sapi.LoadModConfig<WoopConfig>(WoopEssentials.ConfigFile);
            WoopEssentials.Config.Reload(configTemp);
            WoopEssentials.LoadRestartTime(DateTime.Now);
            return "Config reloaded";
        }
        catch (Exception e)
        {
            discord.Sapi.Logger.Error("Error reloading Th3Config: ", e.ToString());
            return "Error reloading Th3Config see server log";
        }
    }
}