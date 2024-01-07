using System.Collections.Generic;
using System.Text;
using Discord;
using Discord.WebSocket;
using Vintagestory.API.Config;

namespace Th3Essentials.Discord.Commands;

public abstract class Serverinfo
{
    public static SlashCommandProperties CreateCommand()
    {
        var serverinfo = new SlashCommandBuilder
        {
            Name = SlashCommands.Serverinfo.ToString().ToLower(),
            Description = Lang.Get("th3essentials:slc-serverinfo")
        };
        return serverinfo.Build();
    }

    public static List<string> HandleSlashCommand(Th3Discord discord, SocketSlashCommand _)
    {
        var re = new List<string>();
        var sb = new StringBuilder();
        sb.Append("Game version: ");
        sb.AppendLine(Th3Util.GetVsVersion());
        sb.Append("Mods:");
        foreach (var mod in discord.Sapi.ModLoader.Mods)
        {
            var modinfo = $"  **{mod.Info.Name}** @ {mod.Info.Version} | {mod.Info.Side}";
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