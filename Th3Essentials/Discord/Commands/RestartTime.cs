using System;
using Discord;
using Discord.WebSocket;
using Vintagestory.API.Config;

namespace Th3Essentials.Discord.Commands
{
    public class RestartTime
    {
        public static void CreateCommand(DiscordSocketClient _client)
        {
            SlashCommandBuilder restartTime = new SlashCommandBuilder
            {
                Name = SlashCommands.RestartTime.ToString().ToLower(),
                Description = Lang.Get("th3essentials:slc-restart")
            };
            _ = _client.Rest.CreateGuildCommand(restartTime.Build(), Th3Essentials.Config.DiscordConfig.GuildId);
        }

        public static string HandleSlashCommand()
        {
            if (Th3Essentials.Config.ShutdownTime != null)
            {
                TimeSpan restart = Th3Util.GetTimeTillRestart();
                return Lang.Get("th3essentials:slc-restart-resp", restart.Hours.ToString("D2"), restart.Minutes.ToString("D2"));
            }
            else
            {
                return Lang.Get("th3essentials:slc-restart-disabled");
            }
        }
    }
}