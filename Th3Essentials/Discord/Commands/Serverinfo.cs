using System.Collections.Generic;
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

        public static List<string> HandleSlashCommand(Th3Discord discord, SocketSlashCommand _)
        {
            List<string> re = new List<string>();
            StringBuilder sb = new StringBuilder();
            sb.Append("Game version: ");
            sb.AppendLine(Th3Util.GetVsVersion());
            sb.Append("Mods:");
            foreach (Mod mod in discord.Sapi.ModLoader.Mods)
            {
                string modinfo = $"  **{mod.Info.Name}** @ {mod.Info.Version} | {mod.Info.Side}";
                if (sb.Length + modinfo.Length >= 1999)
                {
                    re.Add(sb.ToString());
                    sb.Clear();
                }
                else
                {
                    sb.AppendLine();
                }
                sb.Append(modinfo);
            }

            if (sb.Length > 0)
            {
                re.Add(sb.ToString());
            }

            return re;
        }
    }
}