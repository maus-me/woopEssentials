using System.Collections.Generic;
using Discord;
using Discord.WebSocket;
using Vintagestory.API.Config;

namespace Th3Essentials.Discord.Commands;

public abstract class ModifyPermissions
{
    public static SlashCommandProperties CreateCommand()
    {
        List<SlashCommandOptionBuilder> modifypermissionsOptions = new List<SlashCommandOptionBuilder>()
        {
            new()
            {
                Name = "mode",
                Description = Lang.Get("th3essentials:slc-modifypermissions-mode"),
                Type = ApplicationCommandOptionType.String,
                Choices = new List<ApplicationCommandOptionChoiceProperties>(){
                    new(){Name = "add", Value = "add"},
                    new(){Name = "remove", Value = "remove"},
                    new(){Name = "clear", Value = "clear"}
                },
                IsRequired = true,
            },
            new()
            {
                Name = "role",
                Description = Lang.Get("th3essentials:slc-modifypermissions"),
                Type = ApplicationCommandOptionType.Role
            }
        };
        var modifypermissions = new SlashCommandBuilder
        {
            Name = SlashCommands.ModifyPermissions.ToString().ToLower(),
            Description = Lang.Get("th3essentials:slc-modifypermissions"),
            Options = modifypermissionsOptions
        };
        return modifypermissions.Build();
    }

    public static string HandleSlashCommand(Th3Discord discord, SocketSlashCommand commandInteraction)
    {
        string response;
        if (commandInteraction.User is not SocketGuildUser guildUser)
        {
            return "Something went wrong: User was not a GuildUser";
        }

        if (!guildUser.GuildPermissions.Administrator)
        {
            return "You do not have permissions to do that";
        }

        SocketRole? role = null;
        string? mode = null;
        foreach (var option in commandInteraction.Data.Options)
        {
            switch (option.Name)
            {
                case "role":
                {
                    role = option.Value as SocketRole;
                    break;
                }
                case "mode":
                {
                    mode = option.Value as string;
                    break;
                }
            }
        }

        switch (mode)
        {
            case "add":
            {
                if (role != null)
                {
                    discord.Config.ModerationRoles ??= new List<ulong>();
                    discord.Config.ModerationRoles.Add(role.Id);
                    Th3Essentials.Config.MarkDirty();
                    response = $"Added role: {role.Name}";
                }
                else
                {
                    response = "Invalid role";
                }

                break;
            }
            case "remove":
            {
                if (role != null)
                {
                    if (discord.Config.ModerationRoles != null)
                    {
                        if (discord.Config.ModerationRoles.Remove(role.Id))
                        {
                            Th3Essentials.Config.MarkDirty();
                            response = $"Removed role: {role.Name}";
                        }
                        else
                        {
                            response = "Role had not permissions, nothing to remove";
                        }
                    }
                    else
                    {
                        response = "Nothing to remove";
                    }
                }
                else
                {
                    response = "Invalid role";
                }

                break;
            }
            case "clear":
            {
                if (discord.Config.ModerationRoles != null)
                {
                    discord.Config.ModerationRoles.Clear();
                    Th3Essentials.Config.MarkDirty();
                    response = "All moderation roles removed";
                }
                else
                {
                    response = "Nothing to remove";
                }

                break;
            }
            default:
            {
                response = $"Error: Mode option invalid: {mode}";
                break;
            }
        }

        return response;
    }
}