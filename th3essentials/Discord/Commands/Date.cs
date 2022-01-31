using System.Collections.Generic;
using Discord;
using Discord.WebSocket;
using Vintagestory.API.Config;

namespace Th3Essentials.Discord.Commands
{
    public class Date
    {
        public static void CreateCommand(DiscordSocketClient _client)
        {
            SlashCommandBuilder date = new SlashCommandBuilder
            {
                Name = SlashCommands.Date.ToString().ToLower(),
                Description = Lang.Get("th3essentials:slc-date")
            };
            _ = _client.Rest.CreateGuildCommand(date.Build(), Th3Essentials.Config.DiscordConfig.GuildId);
        }
    }
}