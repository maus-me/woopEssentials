using System.Collections.Generic;
using System.Linq;
using Discord;
using Discord.WebSocket;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace Th3Essentials.Discord.Commands;

public abstract class Players
{
    public static SlashCommandProperties CreateCommand()
    {
        var playersOptions = new List<SlashCommandOptionBuilder>()
        {
            new()
            {
                Name = "ping",
                Description = Lang.Get("woopessentials:slc-players-ping"),
                Type = ApplicationCommandOptionType.Boolean
            }
        };
        var players = new SlashCommandBuilder
        {
            Name = SlashCommands.Players.ToString().ToLower(),
            Description = Lang.Get("woopessentials:slc-players"),
            Options = playersOptions
        };
        return players.Build();
    }

    public static string HandleSlashCommand(WoopDiscord discord, SocketSlashCommand commandInteraction)
    {
        bool? ping = null;
        foreach (var option in commandInteraction.Data.Options)
        {
            if (option.Name.Equals("ping"))
            {
                ping = option.Value as bool?;
            }
        }
        var names = new List<string>();
        foreach (var player in discord.Sapi.World.AllOnlinePlayers.Cast<IServerPlayer>())
        {
            if (ping == true)
            {
                names.Add($"{player.PlayerName} ({(int)(player.Ping * 1000)}ms)");
            }
            else
            {
                names.Add(player.PlayerName);
            }
        }
        return names.Count == 0 ? Lang.Get("woopessentials:slc-players-none") : string.Join("\n", names);
    }
}