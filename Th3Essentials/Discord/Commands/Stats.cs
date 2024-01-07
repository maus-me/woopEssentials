using System;
using System.Collections.Generic;
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
        var upSeconds = server.totalUpTime.ElapsedMilliseconds / 1000;
        var upMinutes = 0;
        var upHours = 0;
        var upDays = 0;
        if (upSeconds > 60)
        {
            upMinutes = (int)(upSeconds / 60);
            upSeconds -= 60 * upMinutes;
        }
        if (upMinutes > 60)
        {
            upHours = upMinutes / 60;
            upMinutes -= 60 * upHours;
        }
        if (upHours > 24)
        {
            upDays = upHours / 24;
            upHours -= 24 * upDays;
        }
        stringBuilder.Append("Version: ");
        stringBuilder.AppendLine(Th3Util.GetVsVersion());
        stringBuilder.AppendLine($"Uptime: {upDays} days, {upHours} hours, {upMinutes} minutes, {upSeconds} seconds");
        stringBuilder.AppendLine($"Players online: {server.Clients.Count} / {server.Config.MaxClients}");

        var activeEntities = 0;
        foreach (KeyValuePair<long, Entity> loadedEntity in discord.Sapi.World.LoadedEntities)
        {
            if (loadedEntity.Value.State != EnumEntityState.Inactive)
            {
                activeEntities++;
            }
        }
        stringBuilder.AppendLine($"Memory usage: {decimal.Round(GC.GetTotalMemory(forceFullCollection: false) / (decimal)1048576, 2)} Mb");
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