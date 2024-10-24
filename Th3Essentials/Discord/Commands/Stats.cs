using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Discord;
using Discord.WebSocket;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.Server;

namespace Th3Essentials.Discord.Commands;

public abstract class Stats
{
    public static SlashCommandProperties CreateCommand()
    {
        var stats = new SlashCommandBuilder
        {
            Name = SlashCommands.Stats.ToString().ToLower(),
            Description = Lang.Get("th3essentials:slc-stats")
        };
        return stats.Build();
    }

    public static string HandleSlashCommand(Th3Discord discord, SocketSlashCommand commandInteraction)
    {
        if (commandInteraction.User is not SocketGuildUser guildUser)
            return "Something went wrong: User was not a GuildUser";

        if (!Th3SlashCommands.HasPermission(guildUser, discord.Config.ModerationRoles))
            return "You do not have permissions to do that";
        
        var stringBuilder = new StringBuilder();
        var server = (ServerMain)discord.Sapi.World;
        stringBuilder.Append("Version: ");
        stringBuilder.AppendLine(Th3Util.GetVsVersion());
        stringBuilder.AppendLine($"Uptime: {server.totalUpTime.Elapsed.ToString()}");
        stringBuilder.AppendLine($"Players online: {server.Clients.Count} / {server.Config.MaxClients}");

        var activeEntities = 0;
        foreach (KeyValuePair<long, Entity> loadedEntity in discord.Sapi.World.LoadedEntities)
        {
            if (loadedEntity.Value.State != EnumEntityState.Inactive)
            {
                activeEntities++;
            }
        }
        var managed = decimal.Round((decimal)(GC.GetTotalMemory(false) / 1024f / 1024f), 2).ToString("#.#", GlobalConstants.DefaultCultureInfo);
        var total = decimal.Round((decimal)(Process.GetCurrentProcess().WorkingSet64 / 1024f / 1024f), 2).ToString("#.#", GlobalConstants.DefaultCultureInfo);

        stringBuilder.AppendLine("Memory usage Managed/Total: " + managed + "Mb / " + total + " Mb");
        var statsCollection = server.StatsCollector[GameMath.Mod(server.StatsCollectorIndex - 1, server.StatsCollector.Length)];

        if (statsCollection.ticksTotal > 0)
        {
            stringBuilder.AppendLine($"Last 2s Average Tick Time: {decimal.Round(statsCollection.tickTimeTotal / (decimal)statsCollection.ticksTotal, 2)} ms");
            stringBuilder.AppendLine($"Last 2s Ticks/s: {decimal.Round((decimal)(statsCollection.ticksTotal / 2.0), 2)}");
            stringBuilder.AppendLine($"Last 10 ticks (ms): {string.Join(", ", statsCollection.tickTimes)}");
        }
        stringBuilder.AppendLine($"Loaded chunks: {discord.Sapi.World.LoadedChunkIndices.Length}");
        stringBuilder.AppendLine($"Loaded entities: {discord.Sapi.World.LoadedEntities.Count} ({activeEntities} active)");
        stringBuilder.Append($"Network: {decimal.Round((decimal)(statsCollection.statTotalPackets / 2.0), 2)} Packets/s or {decimal.Round((decimal)(statsCollection.statTotalPacketsLength / 2048.0), 2, MidpointRounding.AwayFromZero)} Kb/s");
        return stringBuilder.ToString();

    }
}