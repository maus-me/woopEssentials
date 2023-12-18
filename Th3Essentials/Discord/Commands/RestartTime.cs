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
            Description = Lang.Get("th3essentials:slc-restart")
        };
        return restartTime.Build();
    }

    public static string HandleSlashCommand()
    {
        if (Th3Essentials.Config.ShutdownEnabled)
        {
            var restart = Th3Essentials.ShutDownTime - DateTime.Now;
            return Lang.Get("th3essentials:slc-restart-resp", restart.Hours.ToString("D2"), restart.Minutes.ToString("D2"));
        }
        else
        {
            return Lang.Get("th3essentials:slc-restart-disabled");
        }
    }
}