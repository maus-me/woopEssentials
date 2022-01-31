using System.Text;
using Discord;
using Discord.WebSocket;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Th3Essentials.Discord.Commands
{
    public class Serverinfo
    {
        public static void CreateCommand(DiscordSocketClient _client)
        {
            SlashCommandBuilder serverinfo = new SlashCommandBuilder
            {
                Name = SlashCommands.Serverinfo.ToString().ToLower(),
                Description = Lang.Get("th3essentials:slc-serverinfo")
            };
            _ = _client.Rest.CreateGuildCommand(serverinfo.Build(), Th3Essentials.Config.DiscordConfig.GuildId);
        }

        public static string HandleSlashCommand(Th3Discord discord, SocketSlashCommand commandInteraction)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Game version: ");
            sb.AppendLine(GameVersion.OverallVersion);
            sb.Append("Mods:");
            foreach (Mod mod in discord.Sapi.ModLoader.Mods)
            {
                sb.AppendLine();
                sb.Append($"  **{mod.Info.Name}** @ {mod.Info.Version} | {mod.Info.Side}");
            }
            return sb.ToString();
        }
    }
}