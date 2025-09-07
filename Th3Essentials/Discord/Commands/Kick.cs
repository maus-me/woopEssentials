using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Vintagestory.API.Config;
using Vintagestory.Server;

namespace Th3Essentials.Discord.Commands;

public abstract class Kick
{
    public static SlashCommandProperties CreateCommand()
    {
        var banOptions = new List<SlashCommandOptionBuilder>()
        {
            new()
            {
                Name = "playername",
                Description = Lang.Get("th3essentials:slc-whitelist-playername"),
                Type = ApplicationCommandOptionType.String,
                IsRequired = true
            },
            new()
            {
                Name = "reason",
                Description = Lang.Get("th3essentials:slc-kick-reason"),
                Type = ApplicationCommandOptionType.String
            }
        };

        var kick = new SlashCommandBuilder
        {
            Name = SlashCommands.Kick.ToString().ToLower(),
            Description = Lang.Get("th3essentials:slc-kick"),
            Options = banOptions
        };
        return kick.Build();
    }

    public static async Task<string> HandleSlashCommand(Th3Discord discord, SocketSlashCommand commandInteraction)
    {
        if (commandInteraction.User is not SocketGuildUser guildUser)
            return "Something went wrong: User was not a GuildUser";

        if (!Th3SlashCommands.HasPermission(guildUser, discord.Config.ModerationRoles))
            return "You do not have permissions to do that";
        
        string? targetPlayer = null;
        string? reason = null;

        foreach (var option in commandInteraction.Data.Options)
        {
            switch (option.Name)
            {
                case "playername":
                {
                    targetPlayer = option.Value as string;
                    break;
                }
                case "reason":
                {
                    reason = option.Value as string;
                    break;
                }
                default:
                {
                    discord.Sapi.Logger.VerboseDebug("Something went wrong getting slc-whitelist/kick option");
                    break;
                }
            }
        }

        if (targetPlayer == null) return "Playername missing";
        
        var playerUid = await Ban.GetPlayerUid(discord.Sapi, targetPlayer);
        if (playerUid == null)
            return $"Could not find player with name: {targetPlayer}";
        
        var player = discord.Sapi.World.AllOnlinePlayers.FirstOrDefault(p => p.PlayerUID.Equals(playerUid));

        if (player is not ServerPlayer splayer)
        {
            return $"{targetPlayer} has been kicked.";
        }

        reason ??= "";
        var hisKickMessage = reason.Length == 0 ? Lang.Get("You've been kicked by {0}", guildUser.DisplayName) : Lang.Get("You've been kicked by {0}, reason: {1}", guildUser.DisplayName, reason);
        var othersKickmessage = reason.Length == 0 ? Lang.Get("{0} has been kicked by {1}", targetPlayer, guildUser.DisplayName) : Lang.Get("{0} has been kicked by {1}, reason: {2}", targetPlayer, guildUser.DisplayName, reason);

        var serverMain = (ServerMain)discord.Sapi.World;
        var client = serverMain.Clients[splayer.ClientId];
        _ = Task.Run(() =>
        {
            try
            {
                serverMain.DisconnectPlayer(client, othersKickmessage, hisKickMessage);
            }
            catch (Exception ex)
            {
                // ignored
            }
        });

        return $"{targetPlayer} has been kicked.";
    }
}