using Discord;
using Vintagestory.API.Config;

namespace Th3Essentials.Discord.Commands
{
    public abstract class Admins
    {
        public static SlashCommandProperties CreateCommand()
        {
            SlashCommandBuilder admins = new SlashCommandBuilder
            {
                Name = SlashCommands.Admins.ToString().ToLower(),
                Description = Lang.Get("th3essentials:slc-admins")
            };
            return admins.Build();
        }
    }
}