using System;
using System.Collections.Generic;
using Discord;
using Discord.WebSocket;
using Vintagestory.API.Config;

namespace Th3Essentials.Discord.Commands;

public abstract class Auth
{
    public static SlashCommandProperties CreateCommand()
    {
        List<SlashCommandOptionBuilder> authOptions = new List<SlashCommandOptionBuilder>()
        {
            new()
            {
                Name = "mode",
                Description = Lang.Get("th3essentials:slc-auth-mode"),
                Type = ApplicationCommandOptionType.String,
                IsRequired = true,
                Choices = new List<ApplicationCommandOptionChoiceProperties>(){
                    new(){Name = "connect", Value = "connect"},
                    new(){Name = "disconnect", Value = "disconnect"}
                },
            }
        };
        var auth = new SlashCommandBuilder
        {
            Name = SlashCommands.Auth.ToString().ToLower(),
            Description = Lang.Get("th3essentials:slc-auth"),
            Options = authOptions
        };
        return auth.Build();
    }

    public static string HandleSlashCommand(SocketSlashCommand commandInteraction)
    {
        if (commandInteraction.User is not SocketGuildUser guildUser)
            return "Auth did not work: Could not get user";

        if (Th3Essentials.Config.DiscordConfig?.Rewards != true)
        {
            return "Auth not enabled";
        }
        
        string? mode = null;
        foreach (var option in commandInteraction.Data.Options)
        {
            mode = option.Name switch
            {
                "mode" => option.Value as string,
                _ => mode
            };
        }
        switch (mode)
        {
            case "connect":
            {
                if (Th3Essentials.Config.DiscordConfig?.LinkedAccounts == null || !Th3Essentials.Config.DiscordConfig.LinkedAccounts.ContainsValue(guildUser.Id.ToString()))
                {
                    var token = Guid.NewGuid().ToString();
                    Th3Discord.AccountsToLink.Add(token, guildUser.Id.ToString());
                    return $"Type `/dcauth {token}` ingame then relog.";
                }
                return "User already linked to a ingame account";
            }
            case "disconnect":
            {
                if (Th3Essentials.Config.DiscordConfig?.LinkedAccounts != null)
                {
                    foreach (KeyValuePair<string, string> account in Th3Essentials.Config.DiscordConfig.LinkedAccounts)
                    {
                        if (account.Value.Equals(guildUser.Id.ToString()))
                        {
                            Th3Essentials.Config.DiscordConfig.LinkedAccounts.Remove(account.Key);
                            Th3Essentials.Config.MarkDirty();
                            return "Your accounts have been disconnected";
                        }
                    }
                }
                return "User not linked to a ingame account";
            }
            default:
            {
                return "Auth mode unknown";
            }
        }
    }
}