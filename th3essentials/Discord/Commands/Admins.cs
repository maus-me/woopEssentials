using System.Collections.Generic;
using Discord;
using Discord.WebSocket;
using Vintagestory.API.Config;

namespace Th3Essentials.Discord.Commands
{
    public class Admins
    {
        public static void CreateCommand(DiscordSocketClient _client)
        {
            SlashCommandBuilder admins = new SlashCommandBuilder
            {
                Name = SlashCommands.Admins.ToString().ToLower(),
                Description = Lang.Get("th3essentials:slc-admins")
            };
            _ = _client.Rest.CreateGuildCommand(admins.Build(), Th3Essentials.Config.DiscordConfig.GuildId);
        }
    }
}