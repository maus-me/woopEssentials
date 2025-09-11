using System;
using Discord;
using Vintagestory.API.Config;

namespace Th3Essentials.Discord.Commands;

public abstract class RestartTime
{
    public static SlashCommandProperties CreateCommand()
    {
        var restartTime = new SlashCommandBuilder
        {
            Name = SlashCommands.RestartTime.ToString().ToLower(),
            Description = Lang.Get("woopessentials:slc-restart")
        };
        return restartTime.Build();
    }

    public static string HandleSlashCommand()
    {
        if (WoopEssentials.Config.ShutdownEnabled)
        {
            var restart = WoopEssentials.ShutDownTime - DateTime.Now;
            return Lang.Get("woopessentials:slc-restart-resp", restart.Hours.ToString("D2"), restart.Minutes.ToString("D2"));
        }

        return Lang.Get("woopessentials:slc-restart-disabled");
    }
}