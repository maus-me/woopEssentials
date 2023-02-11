using System.Collections.Generic;
using System.Linq;
using Discord;
using Discord.WebSocket;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace Th3Essentials.Discord.Commands
{
    public class Players
    {
        public static void CreateCommand(DiscordSocketClient _client)
        {
            List<SlashCommandOptionBuilder> playersOptions = new List<SlashCommandOptionBuilder>()
            {
                new SlashCommandOptionBuilder()
                {
                    Name = "ping",
                    Description = Lang.Get("th3essentials:slc-players-ping"),
                    Type = ApplicationCommandOptionType.Boolean
                }
            };
            SlashCommandBuilder players = new SlashCommandBuilder
            {
                Name = SlashCommands.Players.ToString().ToLower(),
                Description = Lang.Get("th3essentials:slc-players"),
                Options = playersOptions
            };
            _ = _client.Rest.CreateGuildCommand(players.Build(), Th3Essentials.Config.DiscordConfig.GuildId);
        }

        public static string HandleSlashCommand(Th3Discord discord, SocketSlashCommand commandInteraction)
        {
            bool? ping = null;
            foreach (SocketSlashCommandDataOption option in commandInteraction.Data.Options)
            {
                if (option.Name.Equals("ping"))
                {
                    ping = option.Value as bool?;
                }
            }
            List<string> names = new List<string>();
            foreach (IServerPlayer player in discord.Sapi.World.AllOnlinePlayers.Cast<IServerPlayer>())
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
            return names.Count == 0 ? Lang.Get("th3essentials:slc-players-none") : string.Join("\n", names);
        }
    }
}